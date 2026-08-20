using System.IO;
using System.Text.Json;
using ManaChaiLeasing.Data;

namespace ManaChaiLeasing.Services;

public enum AutomaticBackupExecutionStatus
{
    Disabled,
    Success,
    Failed
}

public sealed class AutomaticBackupSettings
{
    public bool IsEnabled { get; set; }

    public string BackupFolder { get; set; } = string.Empty;

    public DateTime? LastAttemptAt { get; set; }

    public DateTime? LastSuccessfulBackupAt { get; set; }

    public string LastSuccessfulBackupPath { get; set; } = string.Empty;

    public string LastError { get; set; } = string.Empty;
}

public sealed record AutomaticBackupExecutionResult(
    AutomaticBackupExecutionStatus Status,
    string? FilePath,
    DateTime? CreatedAt,
    string? ErrorMessage)
{
    public bool IsSuccess =>
        Status == AutomaticBackupExecutionStatus.Success;

    public bool IsFailed =>
        Status == AutomaticBackupExecutionStatus.Failed;
}

public sealed class AutomaticBackupService
{
    public const int RetentionDays = 30;

    private readonly DatabaseBackupService _databaseBackupService = new();

    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            WriteIndented = true
        };

    public AutomaticBackupSettings GetSettings()
    {
        string settingsFile =
            GetSettingsFilePath();

        if (!File.Exists(settingsFile))
        {
            return new AutomaticBackupSettings();
        }

        try
        {
            string json =
                File.ReadAllText(settingsFile);

            AutomaticBackupSettings? settings =
                JsonSerializer.Deserialize<AutomaticBackupSettings>(
                    json,
                    JsonOptions);

            return settings
                ?? new AutomaticBackupSettings();
        }
        catch
        {
            return new AutomaticBackupSettings();
        }
    }

    public AutomaticBackupSettings SaveConfiguration(
        bool isEnabled,
        string backupFolder)
    {
        string normalizedFolder =
            string.IsNullOrWhiteSpace(backupFolder)
                ? string.Empty
                : Path.GetFullPath(
                    backupFolder.Trim());

        if (isEnabled)
        {
            ValidateBackupFolder(
                normalizedFolder);
        }

        AutomaticBackupSettings settings =
            GetSettings();

        settings.IsEnabled = isEnabled;
        settings.BackupFolder = normalizedFolder;

        SaveSettings(
            settings);

        return settings;
    }

    public AutomaticBackupExecutionResult RunAutomaticBackup()
    {
        AutomaticBackupSettings settings =
            GetSettings();

        if (!settings.IsEnabled)
        {
            return new AutomaticBackupExecutionResult(
                AutomaticBackupExecutionStatus.Disabled,
                null,
                null,
                null);
        }

        settings.LastAttemptAt =
            DateTime.Now;

        try
        {
            ValidateBackupFolder(
                settings.BackupFolder);

            Directory.CreateDirectory(
                settings.BackupFolder);

            string destination =
                Path.Combine(
                    settings.BackupFolder,
                    $"ManaChaiLeasing_AutoBackup_{DateTime.Now:yyyyMMdd}.db");

            DatabaseBackupResult result =
                _databaseBackupService.CreateBackup(
                    destination);

            DeleteExpiredAutomaticBackups(
                settings.BackupFolder);

            settings.LastSuccessfulBackupAt =
                result.CreatedAt;

            settings.LastSuccessfulBackupPath =
                result.FilePath;

            settings.LastError =
                string.Empty;

            SaveSettings(
                settings);

            return new AutomaticBackupExecutionResult(
                AutomaticBackupExecutionStatus.Success,
                result.FilePath,
                result.CreatedAt,
                null);
        }
        catch (Exception ex)
        {
            settings.LastError =
                ex.Message;

            try
            {
                SaveSettings(
                    settings);
            }
            catch
            {
                // การบันทึกสถานะ Backup ล้มเหลว
                // ต้องไม่ทำให้รายการธุรกิจที่บันทึกสำเร็จแล้วล้มตาม
            }

            return new AutomaticBackupExecutionResult(
                AutomaticBackupExecutionStatus.Failed,
                null,
                null,
                ex.Message);
        }
    }

    public bool IsRecommendedExternalLocation(
        string backupFolder)
    {
        if (string.IsNullOrWhiteSpace(
                backupFolder))
        {
            return false;
        }

        try
        {
            string backupRoot =
                Path.GetPathRoot(
                    Path.GetFullPath(
                        backupFolder))
                ?? string.Empty;

            string databaseRoot =
                Path.GetPathRoot(
                    Path.GetFullPath(
                        DatabasePaths.DatabaseFile))
                ?? string.Empty;

            return !string.Equals(
                backupRoot,
                databaseRoot,
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static void ValidateBackupFolder(
        string backupFolder)
    {
        if (string.IsNullOrWhiteSpace(
                backupFolder))
        {
            throw new InvalidOperationException(
                "กรุณาเลือกโฟลเดอร์สำหรับสำรองข้อมูลอัตโนมัติ");
        }

        string folder =
            Path.GetFullPath(
                backupFolder);

        string databaseFile =
            Path.GetFullPath(
                DatabasePaths.DatabaseFile);

        string dataDirectory =
            Path.GetDirectoryName(
                databaseFile)
            ?? throw new InvalidOperationException(
                "ไม่พบตำแหน่งฐานข้อมูล");

        DirectoryInfo? appDirectoryInfo =
            Directory.GetParent(
                dataDirectory);

        string applicationDataRoot =
            Path.GetFullPath(
                appDirectoryInfo?.FullName
                ?? dataDirectory);

        string applicationDataRootWithSeparator =
            applicationDataRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        string folderWithSeparator =
            folder.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        if (string.Equals(
                folder,
                applicationDataRoot,
                StringComparison.OrdinalIgnoreCase) ||
            folderWithSeparator.StartsWith(
                applicationDataRootWithSeparator,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ไม่ควรเก็บ Auto Backup ไว้ในโฟลเดอร์ข้อมูลของโปรแกรม กรุณาเลือก Drive หรือโฟลเดอร์อื่น");
        }
    }

    private static void DeleteExpiredAutomaticBackups(
        string backupFolder)
    {
        DateTime cutoff =
            DateTime.Now.Date.AddDays(
                -RetentionDays);

        IEnumerable<string> files =
            Directory.EnumerateFiles(
                backupFolder,
                "ManaChaiLeasing_AutoBackup_*.db",
                SearchOption.TopDirectoryOnly);

        foreach (string file in files)
        {
            try
            {
                FileInfo info =
                    new(file);

                if (info.LastWriteTime < cutoff)
                {
                    info.Delete();
                }
            }
            catch
            {
                // การลบไฟล์เก่าไม่ควรทำให้ Backup ล่าสุดล้มเหลว
            }
        }
    }

    private static void SaveSettings(
        AutomaticBackupSettings settings)
    {
        string settingsFile =
            GetSettingsFilePath();

        string? directory =
            Path.GetDirectoryName(
                settingsFile);

        if (string.IsNullOrWhiteSpace(
                directory))
        {
            throw new InvalidOperationException(
                "ไม่พบตำแหน่งจัดเก็บการตั้งค่า Auto Backup");
        }

        Directory.CreateDirectory(
            directory);

        string tempFile =
            settingsFile + ".tmp";

        string json =
            JsonSerializer.Serialize(
                settings,
                JsonOptions);

        File.WriteAllText(
            tempFile,
            json);

        File.Move(
            tempFile,
            settingsFile,
            overwrite: true);
    }

    private static string GetSettingsFilePath()
    {
        string localAppData =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        return Path.Combine(
            localAppData,
            "ManaChaiLeasing",
            "Config",
            "automatic-backup.json");
    }
}
