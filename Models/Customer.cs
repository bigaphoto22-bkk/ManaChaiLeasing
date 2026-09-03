using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class Customer
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(13)]
    public string? CitizenId { get; set; }

    public int? Age { get; set; }

    [MaxLength(30)]
    public string? Phone { get; set; }

    [MaxLength(1000)]
    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public ICollection<PawnTicket> PawnTickets { get; set; } = new List<PawnTicket>();

    public ICollection<DirectPurchase> DirectPurchases { get; set; } = new List<DirectPurchase>();
}
