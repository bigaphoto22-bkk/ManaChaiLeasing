using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class CustomerProfitSummary
{
    public decimal InterestIncome { get; init; }

    public decimal RedemptionProfit { get; init; }

    public decimal SaleProfit { get; init; }

    public decimal Profit =>
        InterestIncome +
        RedemptionProfit +
        SaleProfit;
}

public sealed class CustomerProfitSummaryService
{
    public CustomerProfitSummary GetSummary(
        int customerId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        if (startDate.HasValue != endDate.HasValue)
        {
            throw new ArgumentException(
                "ต้องระบุวันที่เริ่มต้นและวันที่สิ้นสุดให้ครบ");
        }

        DateTime? start =
            startDate?.Date;

        DateTime? endExclusive =
            endDate?.Date.AddDays(1);

        if (start.HasValue &&
            endDate.HasValue &&
            endDate.Value.Date < start.Value)
        {
            throw new ArgumentException(
                "วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด");
        }

        using AppDbContext db = new();

        IQueryable<PawnTransaction> query =
            db.PawnTransactions
                .AsNoTracking()
                .Include(transaction =>
                    transaction.PawnTicket)
                .Where(transaction =>
                    !transaction.IsVoided &&
                    transaction.PawnTicket.CustomerId ==
                        customerId &&
                    (transaction.TransactionType ==
                        PawnTransactionType.Interest ||
                     transaction.TransactionType ==
                        PawnTransactionType.Redemption ||
                     transaction.TransactionType ==
                        PawnTransactionType.Sale));

        if (start.HasValue &&
            endExclusive.HasValue)
        {
            DateTime periodStart = start.Value;
            DateTime periodEndExclusive =
                endExclusive.Value;

            query = query.Where(transaction =>
                transaction.TransactionDate >=
                    periodStart &&
                transaction.TransactionDate <
                    periodEndExclusive);
        }

        List<PawnTransaction> transactions =
            query.ToList();

        decimal interestIncome = transactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Interest &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction =>
                transaction.Amount);

        decimal redemptionProfit = transactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Redemption &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction =>
                Math.Max(
                    0m,
                    transaction.Amount -
                    transaction.PawnTicket.PrincipalAmount));

        decimal saleProfit = transactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Sale &&
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction =>
                transaction.Amount -
                transaction.PawnTicket.PrincipalAmount);

        return new CustomerProfitSummary
        {
            InterestIncome = interestIncome,
            RedemptionProfit = redemptionProfit,
            SaleProfit = saleProfit
        };
    }
}
