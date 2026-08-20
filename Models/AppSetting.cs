using System.ComponentModel.DataAnnotations;

namespace ManaChaiLeasing.Models;

public class AppSetting
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string StoreName { get; set; } = ManaChaiLeasing.AppInfo.StoreName;

    public decimal InterestRatePercent { get; set; } = 5m;

    public int InterestPeriodDays { get; set; } = 15;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
