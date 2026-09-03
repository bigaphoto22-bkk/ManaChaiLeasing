namespace ManaChaiLeasing.Models;

public enum PawnTicketStatus
{
    Active,
    Redeemed,
    Closed,
    Sold
}

public enum PawnTransactionType
{
    Pawn,
    Interest,
    Redemption,
    Sale
}

public enum CashFlowType
{
    Expense,
    Income
}

public enum DirectPurchaseStatus
{
    InStock,
    Sold,
    Cancelled
}

public enum DirectPurchaseTransactionType
{
    Purchase,
    AdditionalExpense,
    Sale
}
