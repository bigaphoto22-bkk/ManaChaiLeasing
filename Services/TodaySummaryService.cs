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
    public int DirectPurchaseCount { get; init; }
    public int DirectSaleCount { get; init; }

    public decimal PawnExpense { get; init; }
    public decimal InterestIncome { get; init; }
    public decimal RedemptionIncome { get; init; }
    public decimal SaleIncome { get; init; }
    public decimal DirectPurchaseExpense { get; init; }
    public decimal DirectSaleIncome { get; init; }

    public decimal TotalIncome =>
        InterestIncome + RedemptionIncome + SaleIncome + DirectSaleIncome;
    public decimal NetCash => TotalIncome - PawnExpense - DirectPurchaseExpense;

    public List<TodayTransactionRow> Transactions { get; init; } = new();
}

public sealed class TodayTransactionRow
{
    public int? PawnTicketId { get; init; }
    public int? DirectPurchaseId { get; init; }
    public bool IsDirectPurchase => DirectPurchaseId.HasValue;

    public DateTime TransactionDate { get; init; }
    public string TransactionTimeText => TransactionDate.ToString("HH:mm");

    public string TicketNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string ProductSummary { get; init; } = "-";

    public PawnTransactionType TransactionType { get; init; }
    public DirectPurchaseTransactionType? DirectTransactionType { get; init; }
    public string TransactionTypeText => IsDirectPurchase ? DirectTransactionType switch
    {
        DirectPurchaseTransactionType.Purchase => "รับซื้อ",
        DirectPurchaseTransactionType.Sale => "ขายสินค้า",
        DirectPurchaseTransactionType.AdditionalExpense => "ค่าใช้จ่ายเพิ่ม",
        _ => "ซื้อขาย"
    } : TransactionType switch
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
    public string DetailText => IsDirectPurchase ? DirectTransactionType switch
    {
        DirectPurchaseTransactionType.Purchase => "รับซื้อเข้าคลังรอขาย",
        DirectPurchaseTransactionType.Sale => "ขายสินค้าที่รับซื้อ",
        DirectPurchaseTransactionType.AdditionalExpense => "ค่าใช้จ่ายของสินค้า",
        _ => "รายการซื้อขาย"
    } : TransactionType switch
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

        List<DirectPurchaseTransaction> directTransactions =
            db.DirectPurchaseTransactions
                .AsNoTracking()
                .Include(transaction => transaction.DirectPurchase)
                    .ThenInclude(item => item.SellerCustomer)
                .Where(transaction =>
                    !transaction.IsVoided &&
                    transaction.TransactionDate >= start &&
                    transaction.TransactionDate < end)
                .OrderByDescending(transaction => transaction.TransactionDate)
                .ThenByDescending(transaction => transaction.Id)
                .ToList();

        decimal directPurchaseExpense = directTransactions
            .Where(transaction =>
                transaction.TransactionType == DirectPurchaseTransactionType.Purchase &&
                transaction.CashFlowType == CashFlowType.Expense)
            .Sum(transaction => transaction.Amount);

        decimal directSaleIncome = directTransactions
            .Where(transaction =>
                transaction.TransactionType == DirectPurchaseTransactionType.Sale &&
                transaction.CashFlowType == CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        List<TodayTransactionRow> rows = transactions.Select(transaction => new TodayTransactionRow
        {
            PawnTicketId = transaction.PawnTicketId,
            TransactionDate = transaction.TransactionDate,
            TicketNumber = transaction.PawnTicket.TicketNumber,
            CustomerName = $"{transaction.PawnTicket.Customer.FirstName} {transaction.PawnTicket.Customer.LastName}".Trim(),
            ProductSummary = Display(transaction.PawnTicket.ProductSummary),
            TransactionType = transaction.TransactionType,
            CashFlowType = transaction.CashFlowType,
            Amount = transaction.Amount,
            InterestSequence = transaction.InterestSequence,
            PaymentMethod = Display(transaction.PaymentMethod)
        }).Concat(directTransactions.Select(transaction => new TodayTransactionRow
        {
            DirectPurchaseId = transaction.DirectPurchaseId,
            TransactionDate = transaction.TransactionDate,
            TicketNumber = Display(transaction.DirectPurchase.DocumentNumber),
            CustomerName = $"{transaction.DirectPurchase.SellerCustomer.FirstName} {transaction.DirectPurchase.SellerCustomer.LastName}".Trim(),
            ProductSummary = Display(transaction.DirectPurchase.ProductSummary),
            TransactionType = PawnTransactionType.Pawn,
            DirectTransactionType = transaction.TransactionType,
            CashFlowType = transaction.CashFlowType,
            Amount = transaction.Amount,
            PaymentMethod = Display(transaction.PaymentMethod)
        }))
        .OrderByDescending(row => row.TransactionDate)
        .ToList();

        return new TodaySummary
        {
            Date = start,
            TransactionCount = rows.Count,
            PawnCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Pawn),
            InterestCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Interest),
            RedemptionCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Redemption),
            SaleCount = transactions.Count(transaction =>
                transaction.TransactionType == PawnTransactionType.Sale),
            DirectPurchaseCount = directTransactions.Count(transaction =>
                transaction.TransactionType == DirectPurchaseTransactionType.Purchase),
            DirectSaleCount = directTransactions.Count(transaction =>
                transaction.TransactionType == DirectPurchaseTransactionType.Sale),
            PawnExpense = pawnExpense,
            InterestIncome = interestIncome,
            RedemptionIncome = redemptionIncome,
            SaleIncome = saleIncome,
            DirectPurchaseExpense = directPurchaseExpense,
            DirectSaleIncome = directSaleIncome,
            Transactions = rows
        };
    }

    private static string Display(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(cleaned) ? "-" : cleaned;
    }
}
