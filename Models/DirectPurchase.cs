using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class DirectPurchase
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string? DocumentNumber { get; set; }

    public DateTime PurchaseDate { get; set; }

    public decimal PurchasePrice { get; set; }

    public DirectPurchaseStatus Status { get; set; } = DirectPurchaseStatus.InStock;

    public int SellerCustomerId { get; set; }

    public Customer SellerCustomer { get; set; } = null!;

    [MaxLength(100)]
    public string AssetCategory { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ProductType { get; set; }

    [MaxLength(100)]
    public string? Brand { get; set; }

    [MaxLength(200)]
    public string? Model { get; set; }

    [MaxLength(100)]
    public string? CapacityOrSize { get; set; }

    [MaxLength(100)]
    public string? Color { get; set; }

    [MaxLength(150)]
    public string? ImeiOrSerial { get; set; }

    [MaxLength(500)]
    public string? Accessories { get; set; }

    [MaxLength(1000)]
    public string? Condition { get; set; }

    [MaxLength(1500)]
    public string? Specification { get; set; }

    [MaxLength(1500)]
    public string? OtherDetails { get; set; }

    [MaxLength(2500)]
    public string ProductSummary { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    [MaxLength(1500)]
    public string? Note { get; set; }

    [MaxLength(1000)]
    public string? CancellationReason { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<DirectPurchaseTransaction> Transactions { get; set; } = new List<DirectPurchaseTransaction>();

    public ICollection<DirectPurchaseEditAudit> EditAudits { get; set; } = new List<DirectPurchaseEditAudit>();
}
