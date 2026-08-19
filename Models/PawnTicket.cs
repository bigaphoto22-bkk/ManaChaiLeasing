using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class PawnTicket
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string TicketNumber { get; set; } = string.Empty;

    public DateTime PawnDate { get; set; }

    public decimal PrincipalAmount { get; set; }

    // Snapshot เงื่อนไข ณ วันที่สร้างตั๋ว
    // เพื่อให้การเปลี่ยน Settings ในอนาคตไม่แก้สัญญาเก่าย้อนหลัง
    public decimal InterestRatePercent { get; set; } = 5m;

    public int InterestPeriodDays { get; set; } = 15;

    public PawnTicketStatus Status { get; set; } = PawnTicketStatus.Active;

    public int CustomerId { get; set; }

    public Customer Customer { get; set; } = null!;

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

    [MaxLength(1500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<PawnTransaction> Transactions { get; set; } = new List<PawnTransaction>();
}
