namespace ManaChaiLeasing.Models;

public enum PawnTicketStatus
{
    Active,
    Redeemed,
    Closed
}

public enum PawnTransactionType
{
    Pawn,
    Interest,
    Redemption
}

public enum CashFlowType
{
    Expense,
    Income
}
