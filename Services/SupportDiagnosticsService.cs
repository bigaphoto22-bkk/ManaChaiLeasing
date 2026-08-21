using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;

namespace ManaChaiLeasing.Services;

public sealed record SupportPackageResult(
    string FilePath,
    int LogFileCount,
    DateTime CreatedAt);

public sealed class SupportDiagnosticsService
{
    public const int IncludedLogDays = 7;

    private readonly MachineIdentityService _machineIdentityService = new();

    private readonly LicenseValidationService _licenseValidationService = new();

    private readonly AutomaticBackupService _automaticBackupService = new();

    private readonly ThaiIdCardReaderService _thaiIdCardReaderService = new();

    public string LogFolder =>
        AppLog.LogFolder;

    public SupportPackageResult CreateSupportPackage()
    {
        string documents =
            Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

        string outputDirectory =
            Path.Combine(
                documents,
                "ManaChai Support");

        Directory.CreateDirectory(
            outputDirectory);

        string timestamp =
            DateTime.Now.ToString(
                "yyyyMMdd_HHmmss",
                CultureInfo.InvariantCulture);

        string outputFile =
            Path.Combine(
                outputDirectory,
                $"ManaChaiLeasing_Support_{timestamp}.zip");

        if (File.Exists(
                outputFile))
        {
            File.Delete(
                outputFile);
        }

        AppLog.Info(
            "Creating support diagnostics package.");

        int logCount = 0;

        using (
            FileStream fileStream =
                new(
                    outputFile,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.None))
        using (
            ZipArchive archive =
                new(
                    fileStream,
                    ZipArchiveMode.Create,
                    leaveOpen: false))
        {
            AddTextEntry(
                archive,
                "diagnostics.txt",
                BuildDiagnosticsText());

            foreach (string logFile in
                     GetRecentLogFiles())
            {
                try
                {
                    ZipArchiveEntry entry =
                        archive.CreateEntry(
                            $"Logs/{Path.GetFileName(logFile)}",
                            CompressionLevel.Optimal);

                    using Stream source =
                        new FileStream(
                            logFile,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite);

                    using Stream destination =
                        entry.Open();

                    source.CopyTo(
                        destination);

                    logCount++;
                }
                catch (Exception ex)
                {
                    AppLog.Warning(
                        $"Could not add one log file to support package: {ex.Message}");
                }
            }

            AddTextEntry(
                archive,
                "PRIVACY_NOTE.txt",
                """
                ชุดข้อมูล Support นี้สร้างเพื่อวิเคราะห์ปัญหาทางเทคนิคของ ManaChaiLeasing

                โดยตั้งใจไม่รวม:
                - ฐานข้อมูล ManaChaiLeasing.db
                - ไฟล์ Backup .db
                - ข้อมูลลูกค้า / รายละเอียดตั๋วจำนำ
                - ไฟล์ License
                - Vendor Private Key / Password
                - Vendor Key Backup

                ภายในมีเฉพาะข้อมูลระบบพื้นฐานและ Technical Log ของโปรแกรม
                """);
        }

        DateTime createdAt =
            DateTime.Now;

        AppLog.Info(
            $"Support diagnostics package created with {logCount} log file(s).");

        return new SupportPackageResult(
            outputFile,
            logCount,
            createdAt);
    }

    private string BuildDiagnosticsText()
    {
        StringBuilder text =
            new();

        text.AppendLine(
            "ManaChaiLeasing Support Diagnostics");

        text.AppendLine(
            "====================================");

        text.AppendLine(
            $"CreatedLocal: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

        text.AppendLine(
            $"CreatedUtc: {DateTime.UtcNow:O}");

        text.AppendLine(
            $"Application: {AppInfo.ProductVersionText}");

        text.AppendLine(
            "Single Instance: Enabled");

        text.AppendLine(
            "Database Startup Health Check: Enabled");

        text.AppendLine(
            "Duplicate Action Protection: Enabled");

        text.AppendLine(
            $"OS: {RuntimeInformation.OSDescription}");

        text.AppendLine(
            $"OS Architecture: {RuntimeInformation.OSArchitecture}");

        text.AppendLine(
            $"Process Architecture: {RuntimeInformation.ProcessArchitecture}");

        text.AppendLine(
            $".NET Runtime: {RuntimeInformation.FrameworkDescription}");

        text.AppendLine(
            $"Machine Name: {Environment.MachineName}");

        try
        {
            MachineIdentity identity =
                _machineIdentityService.GetIdentity();

            text.AppendLine(
                $"Machine ID: {identity.MachineId}");

            text.AppendLine(
                $"Machine Fingerprint: {identity.FingerprintVersion}");
        }
        catch (Exception ex)
        {
            text.AppendLine(
                $"Machine ID: unavailable ({ex.GetType().Name})");
        }

        try
        {
            LicenseValidationResult license =
                _licenseValidationService.ValidateInstalledLicense();

            text.AppendLine(
                $"License Status: {license.Status}");

            text.AppendLine(
                $"License Type: {license.LicenseTypeText}");

            text.AppendLine(
                $"License Expiry: {license.ExpiryText}");
        }
        catch (Exception ex)
        {
            text.AppendLine(
                $"License Status: unavailable ({ex.GetType().Name})");
        }

        try
        {
            AutomaticBackupSettings backup =
                _automaticBackupService.GetSettings();

            text.AppendLine(
                $"Auto Backup Enabled: {backup.IsEnabled}");

            text.AppendLine(
                $"Auto Backup Last Success: {FormatDate(backup.LastSuccessfulBackupAt)}");

            text.AppendLine(
                $"Auto Backup Last Attempt: {FormatDate(backup.LastAttemptAt)}");

            text.AppendLine(
                $"Auto Backup Has Current Error: {!string.IsNullOrWhiteSpace(backup.LastError)}");
        }
        catch (Exception ex)
        {
            text.AppendLine(
                $"Auto Backup Status: unavailable ({ex.GetType().Name})");
        }

        try
        {
            ThaiIdReaderDetectionResult reader =
                _thaiIdCardReaderService.Detect();

            text.AppendLine(
                "Thai ID Reader Foundation: Enabled");

            text.AppendLine(
                $"Thai ID Reader Status: {reader.Status}");

            text.AppendLine(
                $"Thai ID Reader Count: {reader.ReaderCount}");

            text.AppendLine(
                $"Thai ID Reader Name: {reader.ReaderName ?? "-"}");

            text.AppendLine(
                $"Thai ID Card Present: {reader.Status == ThaiIdReaderStatus.Ready}");
        }
        catch (Exception ex)
        {
            text.AppendLine(
                $"Thai ID Reader Status: unavailable ({ex.GetType().Name})");
        }

        text.AppendLine();
        text.AppendLine(
            "Privacy:");

        text.AppendLine(
            "- Database and backup files are NOT included.");

        text.AppendLine(
            "- Customer and pawn-ticket data are NOT intentionally included.");

        text.AppendLine(
            "- License file and private signing key are NOT included.");

        return text.ToString();
    }

    private static IEnumerable<string> GetRecentLogFiles()
    {
        if (!Directory.Exists(
                AppLog.LogFolder))
        {
            return [];
        }

        DateTime cutoff =
            DateTime.Now.Date.AddDays(
                -(IncludedLogDays - 1));

        return Directory
            .EnumerateFiles(
                AppLog.LogFolder,
                "ManaChaiLeasing_*.log",
                SearchOption.TopDirectoryOnly)
            .Select(
                path => new FileInfo(path))
            .Where(
                info => info.LastWriteTime >= cutoff)
            .OrderBy(
                info => info.Name)
            .Select(
                info => info.FullName)
            .ToList();
    }

    private static void AddTextEntry(
        ZipArchive archive,
        string entryName,
        string content)
    {
        ZipArchiveEntry entry =
            archive.CreateEntry(
                entryName,
                CompressionLevel.Optimal);

        using Stream stream =
            entry.Open();

        using StreamWriter writer =
            new(
                stream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

        writer.Write(
            content);
    }

    private static string FormatDate(
        DateTime? value)
    {
        return value.HasValue
            ? value.Value.ToString(
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture)
            : "-";
    }
}
