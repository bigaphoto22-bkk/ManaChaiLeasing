using System.IO;

using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Data;

public static class DatabaseInitializer
{
    public static string Initialize()
    {
        Directory.CreateDirectory(DatabasePaths.DataDirectory);

        using AppDbContext db = new();
        db.Database.Migrate();

        // Settings เป็นข้อมูล config ไม่ใช่ค่าคำนวณที่ฝังตายตัว
        // สร้างค่าเริ่มต้นเฉพาะกรณีฐานข้อมูลใหม่ยังไม่มี Settings เท่านั้น
        if (!db.AppSettings.Any())
        {
            db.AppSettings.Add(new AppSetting
            {
                StoreName = ManaChaiLeasing.AppInfo.StoreName,
                InterestRatePercent = 5m,
                InterestPeriodDays = 15,
                UpdatedAt = DateTime.Now
            });

            db.SaveChanges();
        }
        else
        {
            // StoreName ยังเก็บใน DB เพื่อรองรับ Schema/Backup เดิม
            // แต่ค่าที่ใช้งานจริงถูกกำหนดจาก AppInfo เพียงจุดเดียว
            AppSetting setting =
                db.AppSettings
                    .OrderBy(item => item.Id)
                    .First();

            if (!string.Equals(
                    setting.StoreName,
                    ManaChaiLeasing.AppInfo.StoreName,
                    StringComparison.Ordinal))
            {
                setting.StoreName =
                    ManaChaiLeasing.AppInfo.StoreName;

                setting.UpdatedAt =
                    DateTime.Now;

                db.SaveChanges();
            }
        }

        return DatabasePaths.DatabaseFile;
    }
}
