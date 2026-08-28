using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class RedemptionPreview
{
    public int PawnTicketId { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public string CustomerName { get; init; } = string.Empty;

    public string ProductSummary { get; init; } = string.Empty;

    public decimal PrincipalAmount { get; init; }

    public decimal InterestRatePercent { get; init; }

    public int InterestPeriodDays { get; init; }

    public int InterestRenewalCount { get; init; }

    public decimal FinalInterestAmount { get; init; }

    public decimal RedemptionTotal { get; init; }

    public DateTime CurrentDueDate { get; init; }

    public string PrincipalAmountText => $"{PrincipalAmount:N2} บาท";

    public string InterestRateText => $"{InterestRatePercent:0.##}%";

    public string InterestPeriodText => $"{InterestPeriodDays:N0} วัน";

    public string FinalInterestAmountText => $"{FinalInterestAmount:N2} บาท";

    public string RedemptionTotalText => $"{RedemptionTotal:N2} บาท";

    public string CurrentDueDateText => CurrentDueDate.ToString("dd/MM/yyyy");

    public string InterestRenewalCountText =>
        $"{InterestRenewalCount:N0} ครั้ง";
}

public sealed record RedemptionResult(
    int PawnTicketId,
    string TicketNumber,
    decimal PrincipalAmount,
    decimal FinalInterestAmount,
    decimal RedemptionTotal,
    DateTime TransactionDate,
    string PaymentMethod);

public sealed class RedemptionService
{
    public RedemptionPreview GetPreview(int pawnTicketId)
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
                "ไม่พบตั๋วจำนำที่ต้องการไถ่ถอน");
        }

        ValidateActiveTicket(ticket);

        return BuildPreview(ticket);
    }

    public RedemptionResult SaveRedemption(
        int pawnTicketId,
        int expectedRenewalCount,
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
                    "ไม่พบตั๋วจำนำที่ต้องการไถ่ถอน");
            }

            ValidateActiveTicket(ticket);

            RedemptionPreview preview =
                BuildPreview(ticket);

            if (preview.InterestRenewalCount !=
                expectedRenewalCount)
            {
                throw new InvalidOperationException(
                    "ข้อมูลตั๋วมีการเปลี่ยนแปลงแล้ว กรุณาปิดหน้าต่างนี้และเปิดรายการใหม่ก่อนทำการไถ่ถอน");
            }

            if (ticket.Transactions.Any(transaction =>
                    !transaction.IsVoided &&
                    transaction.TransactionType ==
                        PawnTransactionType.Redemption))
            {
                throw new InvalidOperationException(
                    "ตั๋วนี้มีรายการไถ่ถอนแล้ว ไม่สามารถบันทึกไถ่ถอนซ้ำได้");
            }

            DateTime now = DateTime.Now;

            string auditNote =
                $"ไถ่ถอน เงินต้น {preview.PrincipalAmount:N2} บาท " +
                $"+ ดอกเบี้ยรอบสุดท้าย {preview.FinalInterestAmount:N2} บาท " +
                $"({preview.InterestRatePercent:0.##}% / " +
                $"{preview.InterestPeriodDays:N0} วัน)";

            string cleanedNote = note?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(cleanedNote))
            {
                auditNote += $" • {cleanedNote}";
            }

            ticket.Transactions.Add(new PawnTransaction
            {
                TransactionType = PawnTransactionType.Redemption,
                CashFlowType = CashFlowType.Income,
                TransactionDate = now,
                Amount = preview.RedemptionTotal,
                PaymentMethod = cleanedPaymentMethod,
                Note = auditNote,
                CreatedAt = now
            });

            ticket.Status = PawnTicketStatus.Redeemed;
            ticket.UpdatedAt = now;

            db.SaveChanges();
            dbTransaction.Commit();

            return new RedemptionResult(
                PawnTicketId: ticket.Id,
                TicketNumber: ticket.TicketNumber,
                PrincipalAmount: preview.PrincipalAmount,
                FinalInterestAmount: preview.FinalInterestAmount,
                RedemptionTotal: preview.RedemptionTotal,
                TransactionDate: now,
                PaymentMethod: cleanedPaymentMethod);
    
        }
}

    private static RedemptionPreview BuildPreview(
        PawnTicket ticket)
    {
        int renewalCount = ticket.Transactions.Count(transaction =>
            !transaction.IsVoided &&
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        decimal finalInterest = Math.Round(
            ticket.PrincipalAmount *
            ticket.InterestRatePercent /
            100m,
            2,
            MidpointRounding.AwayFromZero);

        decimal redemptionTotal =
            ticket.PrincipalAmount +
            finalInterest;

        DateTime currentDueDate =
            PawnTicketDueDateCalculator.Calculate(
                ticket,
                renewalCount);

        return new RedemptionPreview
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
            InterestRenewalCount = renewalCount,
            FinalInterestAmount = finalInterest,
            RedemptionTotal = redemptionTotal,
            CurrentDueDate = currentDueDate
        };
    }

    private static void ValidateActiveTicket(
        PawnTicket ticket)
    {
        if (ticket.Status != PawnTicketStatus.Active)
        {
            throw new InvalidOperationException(
                "ตั๋วนี้ไม่อยู่ในสถานะกำลังจำนำ จึงไม่สามารถไถ่ถอนได้");
        }

        if (ticket.InterestRatePercent <= 0m ||
            ticket.InterestPeriodDays <= 0)
        {
            throw new InvalidOperationException(
                "เงื่อนไขดอกเบี้ยของตั๋วไม่ถูกต้อง กรุณาตรวจสอบข้อมูลก่อนทำรายการ");
        }
    }
}
