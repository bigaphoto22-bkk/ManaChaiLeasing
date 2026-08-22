using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class HomeDashboardSummary
{
    public DateTime StartDate { get; init; }

    public DateTime EndDate { get; init; }

    public DateTime UpdatedAt { get; init; }

    // สถานะ ณ สิ้นวันของ EndDate
    public int ActiveTicketCount { get; init; }

    public int DueAtEndDateCount { get; init; }

    public int OverdueCount { get; init; }

    // ความเคลื่อนไหวภายในช่วง StartDate - EndDate
    public int PawnCount { get; init; }

    public int InterestCount { get; init; }

    public int RedemptionCount { get; init; }

    public int SaleCount { get; init; }

    public decimal PawnExpense { get; init; }

    public decimal InterestIncome { get; init; }

    public decimal RedemptionIncome { get; init; }

    public decimal SaleIncome { get; init; }

    // กำไรจากรายการไถ่ถอน คือเฉพาะส่วนที่เกินเงินต้น
    // (ดอกเบี้ยและค่าธรรมเนียมที่รับจริง)
    public decimal RedemptionProfit { get; init; }

    public decimal SaleProfit { get; init; }

    public decimal TotalIncome =>
        InterestIncome + RedemptionIncome + SaleIncome;

    public decimal TotalExpense => PawnExpense;

    public decimal NetCash =>
        TotalIncome - TotalExpense;

    // เงินต้นที่ปล่อยจำนำและเงินต้นที่รับคืนไม่ใช่กำไร/ขาดทุน
    public decimal Profit =>
        InterestIncome + RedemptionProfit + SaleProfit;
}

public sealed class HomeDashboardService
{
    public HomeDashboardSummary GetSummary()
    {
        DateTime today = DateTime.Today;
        return GetSummary(today, today);
    }

    public HomeDashboardSummary GetSummary(
        DateTime startDate,
        DateTime endDate)
    {
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;

        if (start > end)
        {
            throw new ArgumentException(
                "วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด");
        }

        DateTime endExclusive = end.AddDays(1);

        using AppDbContext db = new();

        // โหลดตั๋วที่เริ่มจำนำไม่เกินวันสิ้นสุด พร้อมประวัติ Transaction
        // เพื่อคำนวณสถานะย้อนหลังจากเหตุการณ์จริง ไม่ใช้ Current Status อย่างเดียว
        List<PawnTicket> tickets = db.PawnTickets
            .AsNoTracking()
            .Include(ticket => ticket.Transactions)
            .Where(ticket => ticket.PawnDate < endExclusive)
            .ToList();

        int activeTicketCount = 0;
        int dueAtEndDateCount = 0;
        int overdueCount = 0;

        foreach (PawnTicket ticket in tickets)
        {
            List<PawnTransaction> transactionsThroughEnd =
                ticket.Transactions
                    .Where(transaction =>
                        !transaction.IsVoided &&
                        transaction.TransactionDate < endExclusive)
                    .ToList();

            bool closedByEnd = transactionsThroughEnd.Any(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Redemption ||
                transaction.TransactionType ==
                    PawnTransactionType.Sale);

            if (closedByEnd)
            {
                continue;
            }

            // ประวัติย้อนหลังใช้ Transaction จริง เพื่อให้ตั๋วที่ถูกไถ่ถอน
            // หรือจำหน่ายในอนาคตยังนับเป็น Active ในอดีตได้ถูกต้อง
            int renewalCount = transactionsThroughEnd.Count(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Interest);

            DateTime dueDate = ticket.PawnDate.Date.AddDays(
                ticket.InterestPeriodDays *
                (renewalCount + 1));

            activeTicketCount++;

            if (dueDate.Date == end)
            {
                dueAtEndDateCount++;
            }
            else if (dueDate.Date < end)
            {
                overdueCount++;
            }
        }

        List<PawnTransaction> periodTransactions = db.PawnTransactions
            .AsNoTracking()
            .Include(transaction => transaction.PawnTicket)
            .Where(transaction =>
                !transaction.IsVoided &&
                transaction.TransactionDate >= start &&
                transaction.TransactionDate < endExclusive)
            .ToList();

        int pawnCount = periodTransactions.Count(transaction =>
            transaction.TransactionType ==
                PawnTransactionType.Pawn);

        int interestCount = periodTransactions.Count(transaction =>
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        int redemptionCount = periodTransactions.Count(transaction =>
            transaction.TransactionType ==
                PawnTransactionType.Redemption);

        int saleCount = periodTransactions.Count(transaction =>
            transaction.TransactionType ==
                PawnTransactionType.Sale);

        decimal pawnExpense = periodTransactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Pawn &&
                transaction.CashFlowType ==
                    CashFlowType.Expense)
            .Sum(transaction => transaction.Amount);

        decimal interestIncome = periodTransactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Interest &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        decimal redemptionIncome = periodTransactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Redemption &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        decimal redemptionProfit = periodTransactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Redemption &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction => Math.Max(
                0m,
                transaction.Amount -
                transaction.PawnTicket.PrincipalAmount));

        decimal saleIncome = periodTransactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Sale &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        decimal saleProfit = periodTransactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Sale &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction =>
                transaction.Amount -
                transaction.PawnTicket.PrincipalAmount);

        return new HomeDashboardSummary
        {
            StartDate = start,
            EndDate = end,
            UpdatedAt = DateTime.Now,
            ActiveTicketCount = activeTicketCount,
            DueAtEndDateCount = dueAtEndDateCount,
            OverdueCount = overdueCount,
            PawnCount = pawnCount,
            InterestCount = interestCount,
            RedemptionCount = redemptionCount,
            SaleCount = saleCount,
            PawnExpense = pawnExpense,
            InterestIncome = interestIncome,
            RedemptionIncome = redemptionIncome,
            SaleIncome = saleIncome,
            RedemptionProfit = redemptionProfit,
            SaleProfit = saleProfit
        };
    }
}
