using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class SmartLookupValue
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string FieldType { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [MaxLength(250)]
    public string Value { get; set; } = string.Empty;

    [MaxLength(250)]
    public string NormalizedValue { get; set; } = string.Empty;

    public int UsageCount { get; set; } = 1;

    public DateTime LastUsedAt { get; set; } = DateTime.Now;
}
