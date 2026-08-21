using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class InterestRenewalPreview
{
    public int PawnTicketId { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string ProductSummary { get; init; } = string.Empty;

    public decimal PrincipalAmount { get; init; }

    public decimal InterestRatePercent { get; init; }

    public int InterestPeriodDays { get; init; }

    public int InterestSequence { get; init; }

    public decimal InterestAmount { get; init; }

    public DateTime CurrentDueDate { get; init; }

    public DateTime NewDueDate { get; init; }

    public string PrincipalAmountText => $"{PrincipalAmount:N2} บาท";

    public string InterestRateText => $"{InterestRatePercent:0.##}%";

    public string InterestPeriodText => $"{InterestPeriodDays:N0} วัน";

    public string InterestSequenceText => $"ครั้งที่ {InterestSequence:N0}";

    public string InterestAmountText => $"{InterestAmount:N2} บาท";

    public string CurrentDueDateText => CurrentDueDate.ToString("dd/MM/yyyy");

    public string NewDueDateText => NewDueDate.ToString("dd/MM/yyyy");
}

public sealed record InterestRenewalResult(
    int PawnTicketId,
    string TicketNumber,
    int InterestSequence,
    decimal InterestAmount,
    DateTime TransactionDate,
    DateTime NewDueDate,
    string PaymentMethod);

public sealed class InterestRenewalService
{
    public InterestRenewalPreview GetPreview(int pawnTicketId)
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
                "ไม่พบตั๋วจำนำที่ต้องการต่อดอก");
        }

        ValidateActiveTicket(ticket);

        return BuildPreview(ticket);
    }

    public InterestRenewalResult SaveRenewal(
        int pawnTicketId,
        int expectedInterestSequence,
        string paymentMethod,
        string? note)
    {
        lock (BusinessTransactionGate.SyncRoot)
        {
            string cleanedPaymentMethod = paymentMethod.Trim();

            if (string.IsNullOrWhiteSpace(cleanedPaymentMethod))
            {
                throw new InvalidOperationException(
                    "กรุณาเลือกช่องทางการชำระเงิน");
            }

            using AppDbContext db = new();
            using var dbTransaction = db.Database.BeginTransaction();

            PawnTicket? ticket = db.PawnTickets
                .Include(item => item.Transactions)
                .SingleOrDefault(item => item.Id == pawnTicketId);

            if (ticket is null)
            {
                throw new InvalidOperationException(
                    "ไม่พบตั๋วจำนำที่ต้องการต่อดอก");
            }

            ValidateActiveTicket(ticket);

            InterestRenewalPreview preview =
                BuildPreview(ticket);

            if (preview.InterestSequence !=
                expectedInterestSequence)
            {
                throw new InvalidOperationException(
                    "ข้อมูลการต่อดอกมีการเปลี่ยนแปลงแล้ว กรุณาปิดหน้าต่างนี้และเปิดรายการใหม่อีกครั้ง");
            }

            if (ticket.Transactions.Any(transaction =>
                    !transaction.IsVoided &&
                    transaction.TransactionType ==
                        PawnTransactionType.Redemption))
            {
                throw new InvalidOperationException(
                    "ตั๋วนี้มีรายการไถ่ถอนแล้ว จึงไม่สามารถต่อดอกซ้ำได้");
            }

            DateTime now = DateTime.Now;

            string auditNote =
                $"ต่อดอก {preview.InterestRatePercent:0.##}% / " +
                $"{preview.InterestPeriodDays:N0} วัน";

            string cleanedNote = note?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(cleanedNote))
            {
                auditNote += $" • {cleanedNote}";
            }

            ticket.Transactions.Add(new PawnTransaction
            {
                TransactionType = PawnTransactionType.Interest,
                CashFlowType = CashFlowType.Income,
                TransactionDate = now,
                Amount = preview.InterestAmount,
                InterestSequence = preview.InterestSequence,
                PaymentMethod = cleanedPaymentMethod,
                Note = auditNote,
                CreatedAt = now
            });

            ticket.UpdatedAt = now;

            db.SaveChanges();
            dbTransaction.Commit();

            return new InterestRenewalResult(
                PawnTicketId: ticket.Id,
                TicketNumber: ticket.TicketNumber,
                InterestSequence: preview.InterestSequence,
                InterestAmount: preview.InterestAmount,
                TransactionDate: now,
                NewDueDate: preview.NewDueDate,
                PaymentMethod: cleanedPaymentMethod);
    
        }
}

    private static InterestRenewalPreview BuildPreview(
        PawnTicket ticket)
    {
        List<PawnTransaction> activeInterestTransactions =
            ticket.Transactions
                .Where(transaction =>
                    !transaction.IsVoided &&
                    transaction.TransactionType ==
                        PawnTransactionType.Interest)
                .ToList();

        int lastSequence = activeInterestTransactions
            .Select(transaction =>
                transaction.InterestSequence ?? 0)
            .DefaultIfEmpty(0)
            .Max();

        int nextSequence = lastSequence + 1;

        decimal interestAmount = Math.Round(
            ticket.PrincipalAmount *
            ticket.InterestRatePercent /
            100m,
            2,
            MidpointRounding.AwayFromZero);

        int completedRenewals =
            activeInterestTransactions.Count;

        DateTime currentDueDate =
            ticket.PawnDate.Date.AddDays(
                ticket.InterestPeriodDays *
                (completedRenewals + 1));

        DateTime newDueDate =
            currentDueDate.AddDays(
                ticket.InterestPeriodDays);

        return new InterestRenewalPreview
        {
            PawnTicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            CustomerName =
                $"{ticket.Customer?.FirstName} " +
                $"{ticket.Customer?.LastName}".Trim(),
            ProductSummary = ticket.ProductSummary,
            PrincipalAmount = ticket.PrincipalAmount,
            InterestRatePercent = ticket.InterestRatePercent,
            InterestPeriodDays = ticket.InterestPeriodDays,
            InterestSequence = nextSequence,
            InterestAmount = interestAmount,
            CurrentDueDate = currentDueDate,
            NewDueDate = newDueDate
        };
    }

    private static void ValidateActiveTicket(
        PawnTicket ticket)
    {
        if (ticket.Status != PawnTicketStatus.Active)
        {
            throw new InvalidOperationException(
                "ตั๋วนี้ไม่อยู่ในสถานะกำลังจำนำ จึงไม่สามารถต่อดอกได้");
        }

        if (ticket.InterestRatePercent <= 0m ||
            ticket.InterestPeriodDays <= 0)
        {
            throw new InvalidOperationException(
                "เงื่อนไขดอกเบี้ยของตั๋วไม่ถูกต้อง กรุณาตรวจสอบข้อมูลก่อนทำรายการ");
        }
    }
}
