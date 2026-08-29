using ManaChaiLeasing.Models;

namespace ManaChaiLeasing.Services;

public static class PawnTicketDueDateCalculator
{
    public static DateTime Calculate(
        PawnTicket ticket)
    {
        int renewalCount = ticket.Transactions.Count(transaction =>
            !transaction.IsVoided &&
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        return Calculate(ticket, renewalCount);
    }

    public static DateTime Calculate(
        PawnTicket ticket,
        int renewalCount)
    {
        int safeRenewalCount =
            Math.Max(0, renewalCount);

        if (ticket.DueDateOverride.HasValue &&
            ticket.DueDateOverrideRenewalCount.HasValue &&
            safeRenewalCount >=
                ticket.DueDateOverrideRenewalCount.Value)
        {
            int additionalRenewals =
                safeRenewalCount -
                ticket.DueDateOverrideRenewalCount.Value;

            return ticket.DueDateOverride.Value.Date.AddDays(
                ticket.InterestPeriodDays *
                additionalRenewals);
        }

        return ticket.PawnDate.Date.AddDays(
            ticket.InterestPeriodDays *
            (safeRenewalCount + 1));
    }
}
