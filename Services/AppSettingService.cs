using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class AppSettingService
{
    public AppSetting GetSettings()
    {
        using AppDbContext db = new();

        AppSetting? setting = db.AppSettings
            .AsNoTracking()
            .OrderBy(item => item.Id)
            .FirstOrDefault();

        if (setting is not null)
        {
            return setting;
        }

        AppSetting defaultSetting = new()
        {
            StoreName = ManaChaiLeasing.AppInfo.StoreName,
            InterestRatePercent = 5m,
            InterestPeriodDays = 15,
            UpdatedAt = DateTime.Now
        };

        db.AppSettings.Add(defaultSetting);
        db.SaveChanges();

        return defaultSetting;
    }

    public AppSetting SaveSettings(
        decimal interestRatePercent,
        int interestPeriodDays)
    {
        if (interestRatePercent <= 0m ||
            interestRatePercent > 100m)
        {
            throw new InvalidOperationException(
                "อัตราดอกเบี้ยต้องมากกว่า 0 และไม่เกิน 100%");
        }

        if (interestPeriodDays < 1 ||
            interestPeriodDays > 365)
        {
            throw new InvalidOperationException(
                "จำนวนวันต่อรอบต้องอยู่ระหว่าง 1 - 365 วัน");
        }

        using AppDbContext db = new();

        AppSetting? setting = db.AppSettings
            .OrderBy(item => item.Id)
            .FirstOrDefault();

        if (setting is null)
        {
            setting = new AppSetting();
            db.AppSettings.Add(setting);
        }

        setting.StoreName = ManaChaiLeasing.AppInfo.StoreName;
        setting.InterestRatePercent = interestRatePercent;
        setting.InterestPeriodDays = interestPeriodDays;
        setting.UpdatedAt = DateTime.Now;

        db.SaveChanges();

        return setting;
    }

    public decimal CalculateInterestForOnePeriod(
        decimal principalAmount,
        decimal interestRatePercent)
    {
        decimal interest =
            principalAmount *
            interestRatePercent /
            100m;

        return Math.Round(
            interest,
            2,
            MidpointRounding.AwayFromZero);
    }
}
