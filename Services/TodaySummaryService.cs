using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class TodaySummary
{
    public DateTime Date { get; init; }
    public int TransactionCount { get; init; }
    public int PawnCount { get; init; }
    public int InterestCount { get; init; }
    public int RedemptionCount { get; init; }
    public int SaleCount { get; init; }

    public decimal PawnExpense { get; init; }
    public decimal InterestIncome { get; init; }
    public decimal RedemptionIncome { get; init; }
    public decimal SaleIncome { get; init; }

    public decimal TotalIncome =>
        InterestIncome + RedemptionIncome + SaleIncome;
    public decimal NetCash => TotalIncome - PawnExpense;

    public List<TodayTransactionRow> Transactions { get; init; } = new();
}

public sealed class TodayTransactionRow
{
    public int PawnTicketId { get; init; }

    public DateTime TransactionDate { get; init; }
    public string TransactionTimeText => TransactionDate.ToString("HH:mm");

    public string TicketNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ProductSummary { get; init; } = "-";

    public PawnTransactionType TransactionType { get; init; }
    public string TransactionTypeText => TransactionType switch
    {
        PawnTransactionType.Pawn => "จำนำ",
        PawnTransactionType.Interest => "ต่อดอก",
        PawnTransactionType.Redemption => "ไถ่ถอน",
        PawnTransactionType.Sale => "จำหน่าย",
        _ => TransactionType.ToString()
    };

    public CashFlowType CashFlowType { get; init; }
    public string CashFlowText => CashFlowType switch
    {
        CashFlowType.Expense => "จ่ายออก",
        CashFlowType.Income => "รับเข้า",
        _ => CashFlowType.ToString()
    };

    public decimal Amount { get; init; }
    public string AmountText => $"{Amount:N2}";

    public int? InterestSequence { get; init; }
    public string DetailText => TransactionType switch
    {
        PawnTransactionType.Interest when InterestSequence.HasValue =>
            $"ต่อดอกครั้งที่ {InterestSequence.Value:N0}",
        PawnTransactionType.Redemption => "ปิดตั๋ว",
        PawnTransactionType.Sale => "จำหน่ายสินค้า",
        PawnTransactionType.Pawn => "รับจำนำใหม่",
        _ => "-"
    };

    public string PaymentMethod { get; init; } = "-";
}

public sealed class TodaySummaryService
{
    public TodaySummary GetTodaySummary()
    {
        DateTime start = DateTime.Today;
        DateTime end = start.AddDays(1);

        using AppDbContext db = new();

        List<PawnTransaction> transactions = db.PawnTransactions
            .AsNoTracking()
            .Include(transaction => transaction.PawnTicket)
                .ThenInclude(ticket => ticket.Customer)
            .Where(transaction =>
                !transaction.IsVoided &&
                transaction.TransactionDate >= start &&
                transaction.TransactionDate < end)
            .OrderByDescending(transaction => transaction.TransactionDate)
            .ThenByDescending(transaction => transaction.Id)
            .ToList();

        decimal pawnExpense = transactions
            .Where(transaction =>
                transaction.TransactionType == PawnTransactionType.Pawn &&
                transaction.CashFlowType == CashFlowType.Expense)
            .Sum(transaction => transaction.Amount);

        decimal interestIncome = transactions
            .Where(transaction =>
                transaction.TransactionType == PawnTransactionType.Interest &&
                transaction.CashFlowType == CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        decimal redemptionIncome = transactions
            .Where(transaction =>
                transaction.TransactionType == PawnTransactionType.Redemption &&
                transaction.CashFlowType == CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        decimal saleIncome = transactions
            .Where(transaction =>
                transaction.TransactionType == PawnTransactionType.Sale &&
                transaction.CashFlowType == CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        return new TodaySummary
        {
            Date = start,
            TransactionCount = transactions.Count,
            PawnCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Pawn),
            InterestCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Interest),
            RedemptionCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Redemption),
            SaleCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Sale),
            PawnExpense = pawnExpense,
            InterestIncome = interestIncome,
            RedemptionIncome = redemptionIncome,
            SaleIncome = saleIncome,
            Transactions = transactions.Select(transaction => new TodayTransactionRow
            {
                PawnTicketId = transaction.PawnTicketId,
                TransactionDate = transaction.TransactionDate,
                TicketNumber = transaction.PawnTicket.TicketNumber,
                CustomerName =
                    $"{transaction.PawnTicket.Customer.FirstName} " +
                    $"{transaction.PawnTicket.Customer.LastName}".Trim(),
                ProductSummary =
                    Display(
                        transaction.PawnTicket.ProductSummary),
                TransactionType = transaction.TransactionType,
                CashFlowType = transaction.CashFlowType,
                Amount = transaction.Amount,
                InterestSequence = transaction.InterestSequence,
                PaymentMethod = Display(transaction.PaymentMethod)
            }).ToList()
        };
    }

    private static string Display(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(cleaned) ? "-" : cleaned;
    }
}
