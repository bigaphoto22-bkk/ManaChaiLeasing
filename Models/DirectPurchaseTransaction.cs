using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class DirectPurchaseTransaction
{
    public int Id { get; set; }

    public int DirectPurchaseId { get; set; }

    public DirectPurchase DirectPurchase { get; set; } = null!;

    public DirectPurchaseTransactionType TransactionType { get; set; }

    public CashFlowType CashFlowType { get; set; }

    public DateTime TransactionDate { get; set; }

    public decimal Amount { get; set; }

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(1000)]
    public string? Note { get; set; }

    public bool IsVoided { get; set; }

    [MaxLength(500)]
    public string? VoidReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
