using System.Globalization;
using System.IO;
using System.Text;

namespace ManaChaiLeasing.Services;

public static class AppLog
{
    public const int RetentionDays = 30;

    private static readonly object SyncRoot = new();

    public static string LogFolder =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ManaChaiLeasing",
            "Logs");

    public static string CurrentLogFile =>
        Path.Combine(
            LogFolder,
            $"ManaChaiLeasing_{DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}.log");

    public static void Initialize()
    {
        try
        {
            Directory.CreateDirectory(
                LogFolder);

            DeleteExpiredLogs();

            Info(
                $"Session start • {AppInfo.ProductVersionText} • OS {Environment.OSVersion}");
        }
        catch
        {
            // Logging ต้องไม่ทำให้โปรแกรมเปิดไม่ได้
        }
    }

    public static void Info(
        string message)
    {
        Write(
            "INFO",
            message,
            null);
    }

    public static void Warning(
        string message)
    {
        Write(
            "WARN",
            message,
            null);
    }

    public static void Error(
        string message,
        Exception? exception = null)
    {
        Write(
            "ERROR",
            message,
            exception);
    }

    public static void Critical(
        string message,
        Exception? exception = null)
    {
        Write(
            "CRITICAL",
            message,
            exception);
    }

    private static void Write(
        string level,
        string message,
        Exception? exception)
    {
        try
        {
            Directory.CreateDirectory(
                LogFolder);

            StringBuilder line =
                new();

            line.Append(
                DateTime.Now.ToString(
                    "yyyy-MM-dd HH:mm:ss.fff",
                    CultureInfo.InvariantCulture));

            line.Append(" [");
            line.Append(level);
            line.Append("] ");
            line.AppendLine(
                SanitizeMessage(
                    message));

            if (exception is not null)
            {
                line.AppendLine(
                    exception.ToString());
            }

            lock (SyncRoot)
            {
                File.AppendAllText(
                    CurrentLogFile,
                    line.ToString(),
                    Encoding.UTF8);
            }
        }
        catch
        {
            // Error logger ห้าม throw กลับไปทำให้ business operation ล้ม
        }
    }

    private static string SanitizeMessage(
        string message)
    {
        if (string.IsNullOrWhiteSpace(
                message))
        {
            return "-";
        }

        // Log technical context only.
        // Caller ต้องไม่ส่งข้อมูลลูกค้า/เลขบัตร/รายละเอียดตั๋วมาใน message.
        return message
            .Replace(
                "\r",
                " ")
            .Replace(
                "\n",
                " ");
    }

    private static void DeleteExpiredLogs()
    {
        DateTime cutoff =
            DateTime.Now.Date.AddDays(
                -RetentionDays);

        foreach (string file in
                 Directory.EnumerateFiles(
                     LogFolder,
                     "ManaChaiLeasing_*.log",
                     SearchOption.TopDirectoryOnly))
        {
            try
            {
                FileInfo info =
                    new(file);

                if (info.LastWriteTime <
                    cutoff)
                {
                    info.Delete();
                }
            }
            catch
            {
                // Housekeeping failure ไม่กระทบการ Logging ปัจจุบัน
            }
        }
    }
}
