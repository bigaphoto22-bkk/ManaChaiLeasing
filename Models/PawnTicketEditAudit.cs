using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class PawnTicketEditAudit
{
    public int Id { get; set; }

    public int PawnTicketId { get; set; }

    public PawnTicket PawnTicket { get; set; } = null!;

    public DateTime EditedAt { get; set; } = DateTime.Now;

    [MaxLength(200)]
    public string EditorUser { get; set; } = string.Empty;

    [MaxLength(100)]
    public string EditorMachine { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(12000)]
    public string ChangeSummary { get; set; } = string.Empty;
}
