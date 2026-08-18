using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class PawnTransaction
{
    public int Id { get; set; }

    public int PawnTicketId { get; set; }

    public PawnTicket PawnTicket { get; set; } = null!;

    public PawnTransactionType TransactionType { get; set; }

    public CashFlowType CashFlowType { get; set; }

    public DateTime TransactionDate { get; set; }

    public decimal Amount { get; set; }

    public int? InterestSequence { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsVoided { get; set; }

    [MaxLength(500)]
    public string? VoidReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
