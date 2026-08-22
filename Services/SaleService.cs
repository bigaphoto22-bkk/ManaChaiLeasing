using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class SalePreview
{
    public int PawnTicketId { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string ProductSummary { get; init; } = string.Empty;

    public decimal PrincipalAmount { get; init; }

    public int InterestRenewalCount { get; init; }

    public DateTime CurrentDueDate { get; init; }

    public int OverdueDays =>
        Math.Max(0, (DateTime.Today - CurrentDueDate.Date).Days);

    public string PrincipalAmountText =>
        $"{PrincipalAmount:N2} บาท";

    public string CurrentDueDateText =>
        CurrentDueDate.ToString("dd/MM/yyyy");

    public string OverdueText =>
        $"เกินกำหนด {OverdueDays:N0} วัน";
}

public sealed record SaleResult(
    int PawnTicketId,
    string TicketNumber,
    decimal PrincipalAmount,
    decimal SaleAmount,
    decimal Profit,
    DateTime TransactionDate,
    string PaymentMethod);

public sealed class SaleService
{
    public SalePreview GetPreview(int pawnTicketId)
    {
        using AppDbContext db = new();

        PawnTicket? ticket = db.PawnTickets
            .AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.Transactions)
            .SingleOrDefault(item => item.Id == pawnTicketId);

        if (ticket is null)
        {
            throw new InvalidOperationException(
                "ไม่พบตั๋วจำนำที่ต้องการจำหน่าย");
        }

        ValidateEligibleTicket(ticket);

        return BuildPreview(ticket);
    }

    public SaleResult SaveSale(
        int pawnTicketId,
        int expectedRenewalCount,
        DateTime saleDate,
        decimal saleAmount,
        string paymentMethod,
        string? note)
    {
        lock (BusinessTransactionGate.SyncRoot)
        {
            if (saleAmount <= 0m)
            {
                throw new InvalidOperationException(
                    "ราคาจำหน่ายต้องมากกว่า 0 บาท");
            }

            string cleanedPaymentMethod = paymentMethod.Trim();

            if (string.IsNullOrWhiteSpace(cleanedPaymentMethod))
            {
                throw new InvalidOperationException(
                    "กรุณาเลือกช่องทางการรับเงิน");
            }

            DateTime today = DateTime.Today;

            if (saleDate.Date > today)
            {
                throw new InvalidOperationException(
                    "วันที่จำหน่ายต้องไม่เกินวันนี้");
            }

            using AppDbContext db = new();
            using var dbTransaction = db.Database.BeginTransaction();

            PawnTicket? ticket = db.PawnTickets
                .Include(item => item.Customer)
                .Include(item => item.Transactions)
                .SingleOrDefault(item => item.Id == pawnTicketId);

            if (ticket is null)
            {
                throw new InvalidOperationException(
                    "ไม่พบตั๋วจำนำที่ต้องการจำหน่าย");
            }

            ValidateEligibleTicket(ticket);

            SalePreview preview = BuildPreview(ticket);

            if (preview.InterestRenewalCount != expectedRenewalCount)
            {
                throw new InvalidOperationException(
                    "ข้อมูลตั๋วมีการเปลี่ยนแปลงแล้ว กรุณาปิดหน้าต่างนี้และเปิดรายการใหม่ก่อนจำหน่าย");
            }

            if (saleDate.Date <= preview.CurrentDueDate.Date)
            {
                throw new InvalidOperationException(
                    $"วันที่จำหน่ายต้องอยู่หลังวันครบกำหนด {preview.CurrentDueDate:dd/MM/yyyy}");
            }

            if (ticket.Transactions.Any(transaction =>
                    !transaction.IsVoided &&
                    (transaction.TransactionType ==
                        PawnTransactionType.Redemption ||
                     transaction.TransactionType ==
                        PawnTransactionType.Sale)))
            {
                throw new InvalidOperationException(
                    "ตั๋วนี้ถูกไถ่ถอนหรือจำหน่ายแล้ว ไม่สามารถบันทึกซ้ำได้");
            }

            DateTime now = DateTime.Now;
            DateTime transactionDate = saleDate.Date == today
                ? now
                : saleDate.Date.Add(now.TimeOfDay);

            decimal profit =
                saleAmount - ticket.PrincipalAmount;

            string profitLabel = profit >= 0m
                ? "กำไร"
                : "ขาดทุน";

            string auditNote =
                $"จำหน่ายสินค้า ราคาขาย {saleAmount:N2} บาท • " +
                $"เงินต้น {ticket.PrincipalAmount:N2} บาท • " +
                $"{profitLabel} {Math.Abs(profit):N2} บาท";

            string cleanedNote = note?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(cleanedNote))
            {
                auditNote += $" • {cleanedNote}";
            }

            ticket.Transactions.Add(new PawnTransaction
            {
                TransactionType = PawnTransactionType.Sale,
                CashFlowType = CashFlowType.Income,
                TransactionDate = transactionDate,
                Amount = saleAmount,
                PaymentMethod = cleanedPaymentMethod,
                Note = auditNote,
                CreatedAt = now
            });

            ticket.Status = PawnTicketStatus.Sold;
            ticket.UpdatedAt = now;

            db.SaveChanges();
            dbTransaction.Commit();

            return new SaleResult(
                PawnTicketId: ticket.Id,
                TicketNumber: ticket.TicketNumber,
                PrincipalAmount: ticket.PrincipalAmount,
                SaleAmount: saleAmount,
                Profit: profit,
                TransactionDate: transactionDate,
                PaymentMethod: cleanedPaymentMethod);
        }
    }

    private static SalePreview BuildPreview(PawnTicket ticket)
    {
        int renewalCount = ticket.Transactions.Count(transaction =>
            !transaction.IsVoided &&
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        DateTime currentDueDate =
            ticket.PawnDate.Date.AddDays(
                ticket.InterestPeriodDays *
                (renewalCount + 1));

        return new SalePreview
        {
            PawnTicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            CustomerName =
                $"{ticket.Customer?.FirstName} " +
                $"{ticket.Customer?.LastName}".Trim(),
            ProductSummary = ticket.ProductSummary,
            PrincipalAmount = ticket.PrincipalAmount,
            InterestRenewalCount = renewalCount,
            CurrentDueDate = currentDueDate
        };
    }

    private static void ValidateEligibleTicket(PawnTicket ticket)
    {
        if (ticket.Status != PawnTicketStatus.Active)
        {
            throw new InvalidOperationException(
                "ตั๋วนี้ไม่อยู่ในสถานะกำลังจำนำ จึงไม่สามารถจำหน่ายได้");
        }

        if (ticket.Transactions.Any(transaction =>
                !transaction.IsVoided &&
                (transaction.TransactionType ==
                    PawnTransactionType.Redemption ||
                 transaction.TransactionType ==
                    PawnTransactionType.Sale)))
        {
            throw new InvalidOperationException(
                "ตั๋วนี้ถูกไถ่ถอนหรือจำหน่ายแล้ว");
        }

        int renewalCount = ticket.Transactions.Count(transaction =>
            !transaction.IsVoided &&
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        DateTime currentDueDate =
            ticket.PawnDate.Date.AddDays(
                ticket.InterestPeriodDays *
                (renewalCount + 1));

        if (currentDueDate.Date >= DateTime.Today)
        {
            throw new InvalidOperationException(
                $"ตั๋วยังไม่เกินกำหนด (ครบกำหนด {currentDueDate:dd/MM/yyyy}) จึงยังจำหน่ายไม่ได้");
        }
    }
}
