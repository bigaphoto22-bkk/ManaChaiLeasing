using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class HomeDashboardSummary
{
    public DateTime SummaryDate { get; init; }

    public DateTime UpdatedAt { get; init; }

    public int ActiveTicketCount { get; init; }

    public int DueTodayCount { get; init; }

    public int InterestTodayCount { get; init; }

    public decimal PawnExpenseToday { get; init; }

    public decimal IncomeToday { get; init; }

    public decimal NetCashToday =>
        IncomeToday - PawnExpenseToday;
}

public sealed class HomeDashboardService
{
    public HomeDashboardSummary GetSummary()
    {
        DateTime today = DateTime.Today;
        DateTime tomorrow = today.AddDays(1);

        using AppDbContext db = new();

        List<PawnTicket> activeTickets = db.PawnTickets
            .AsNoTracking()
            .Include(ticket => ticket.Transactions)
            .Where(ticket =>
                ticket.Status == PawnTicketStatus.Active)
            .ToList();

        int dueTodayCount = activeTickets.Count(ticket =>
        {
            int renewalCount = ticket.Transactions.Count(transaction =>
                !transaction.IsVoided &&
                transaction.TransactionType ==
                    PawnTransactionType.Interest);

            DateTime currentDueDate =
                ticket.PawnDate.Date.AddDays(
                    ticket.InterestPeriodDays *
                    (renewalCount + 1));

            return currentDueDate.Date == today;
        });

        List<PawnTransaction> todayTransactions = db.PawnTransactions
            .AsNoTracking()
            .Where(transaction =>
                !transaction.IsVoided &&
                transaction.TransactionDate >= today &&
                transaction.TransactionDate < tomorrow)
            .ToList();

        int interestTodayCount = todayTransactions.Count(transaction =>
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        decimal pawnExpenseToday = todayTransactions
            .Where(transaction =>
                transaction.TransactionType ==
                    PawnTransactionType.Pawn &&
                transaction.CashFlowType ==
                    CashFlowType.Expense)
            .Sum(transaction => transaction.Amount);

        decimal incomeToday = todayTransactions
            .Where(transaction =>
                transaction.CashFlowType ==
                    CashFlowType.Income)
            .Sum(transaction => transaction.Amount);

        return new HomeDashboardSummary
        {
            SummaryDate = today,
            UpdatedAt = DateTime.Now,
            ActiveTicketCount = activeTickets.Count,
            DueTodayCount = dueTodayCount,
            InterestTodayCount = interestTodayCount,
            PawnExpenseToday = pawnExpenseToday,
            IncomeToday = incomeToday
        };
    }
}
