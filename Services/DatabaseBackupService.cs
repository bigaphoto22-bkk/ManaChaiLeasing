using System.IO;
using ManaChaiLeasing.Data;
using Microsoft.Data.Sqlite;

namespace ManaChaiLeasing.Services;

public sealed record DatabaseBackupResult(
    string FilePath,
    long FileSizeBytes,
    DateTime CreatedAt);

public sealed record DatabaseBackupInfo(
    string FilePath,
    string StoreName,
    int CustomerCount,
    int PawnTicketCount,
    int TransactionCount,
    DateTime FileModifiedAt);

public sealed record DatabaseRestoreResult(
    string RestoredFromPath,
    string SafetyBackupPath);

public sealed class DatabaseBackupService
{
    public DatabaseBackupResult CreateBackup(
        string destinationPath)
    {
        string destination =
            Path.GetFullPath(destinationPath);

        string liveDatabase =
            Path.GetFullPath(DatabasePaths.DatabaseFile);

        if (string.Equals(
                destination,
                liveDatabase,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ไม่สามารถบันทึก Backup ทับฐานข้อมูลที่กำลังใช้งานอยู่ได้");
        }

        string? directory =
            Path.GetDirectoryName(destination);

        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException(
                "ตำแหน่งจัดเก็บ Backup ไม่ถูกต้อง");
        }

        Directory.CreateDirectory(directory);

        if (File.Exists(destination))
        {
            File.Delete(destination);
        }

        using SqliteConnection source =
            new(DatabasePaths.ConnectionString);

        using SqliteConnection backup =
            new(BuildConnectionString(
                destination,
                SqliteOpenMode.ReadWriteCreate));

        source.Open();
        backup.Open();

        // SQLite Online Backup API:
        // ได้ snapshot ที่สอดคล้องกัน แม้ตัวโปรแกรมกำลังเปิดอยู่
        source.BackupDatabase(backup);

        backup.Close();
        source.Close();

        ValidateDatabaseFile(destination);

        FileInfo fileInfo =
            new(destination);

        return new DatabaseBackupResult(
            destination,
            fileInfo.Length,
            DateTime.Now);
    }

    public DatabaseBackupInfo InspectBackup(
        string sourcePath)
    {
        string source =
            Path.GetFullPath(sourcePath);

        ValidateDatabaseFile(source);

        using SqliteConnection connection =
            new(BuildConnectionString(
                source,
                SqliteOpenMode.ReadOnly));

        connection.Open();

        string storeName =
            ExecuteString(
                connection,
                "SELECT StoreName FROM AppSettings ORDER BY Id LIMIT 1;")
            ?? "-";

        int customerCount =
            ExecuteCount(
                connection,
                "SELECT COUNT(*) FROM Customers;");

        int pawnTicketCount =
            ExecuteCount(
                connection,
                "SELECT COUNT(*) FROM PawnTickets;");

        int transactionCount =
            ExecuteCount(
                connection,
                "SELECT COUNT(*) FROM PawnTransactions;");

        FileInfo fileInfo =
            new(source);

        return new DatabaseBackupInfo(
            source,
            storeName,
            customerCount,
            pawnTicketCount,
            transactionCount,
            fileInfo.LastWriteTime);
    }

    public DatabaseRestoreResult RestoreBackup(
        string sourcePath)
    {
        string source =
            Path.GetFullPath(sourcePath);

        string liveDatabase =
            Path.GetFullPath(DatabasePaths.DatabaseFile);

        if (string.Equals(
                source,
                liveDatabase,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "ไฟล์ที่เลือกคือฐานข้อมูลที่โปรแกรมกำลังใช้งานอยู่ ไม่จำเป็นต้องกู้คืน");
        }

        // ต้องตรวจไฟล์ก่อนแตะฐานข้อมูลจริง
        ValidateDatabaseFile(source);

        string backupDirectory =
            GetSafetyBackupDirectory();

        Directory.CreateDirectory(
            backupDirectory);

        string safetyBackupPath =
            Path.Combine(
                backupDirectory,
                $"BeforeRestore_{DateTime.Now:yyyyMMdd_HHmmss}.db");

        // สำรองข้อมูลปัจจุบันแบบ Online Backup ก่อน Restore ทุกครั้ง
        CreateBackup(
            safetyBackupPath);

        string dataDirectory =
            Path.GetDirectoryName(liveDatabase)
            ?? throw new InvalidOperationException(
                "ไม่พบตำแหน่งฐานข้อมูลปัจจุบัน");

        Directory.CreateDirectory(
            dataDirectory);

        string tempRestorePath =
            Path.Combine(
                dataDirectory,
                $"restore_{Guid.NewGuid():N}.tmp");

        string rollbackPath =
            Path.Combine(
                dataDirectory,
                $"restore_rollback_{Guid.NewGuid():N}.db");

        try
        {
            // เอาไฟล์ Backup มาวางเป็น temp บน drive เดียวกับฐานข้อมูลจริง
            File.Copy(
                source,
                tempRestorePath,
                overwrite: true);

            ValidateDatabaseFile(
                tempRestorePath);

            // ปิด pooled SQLite connections ก่อนสลับไฟล์ฐานข้อมูล
            SqliteConnection.ClearAllPools();

            DeleteIfExists(
                liveDatabase + "-wal");

            DeleteIfExists(
                liveDatabase + "-shm");

            if (File.Exists(liveDatabase))
            {
                File.Replace(
                    tempRestorePath,
                    liveDatabase,
                    rollbackPath,
                    ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(
                    tempRestorePath,
                    liveDatabase);
            }

            DeleteIfExists(
                liveDatabase + "-wal");

            DeleteIfExists(
                liveDatabase + "-shm");

            // ตรวจฐานข้อมูลหลังสลับไฟล์อีกครั้ง
            ValidateDatabaseFile(
                liveDatabase);

            DeleteIfExists(
                rollbackPath);

            return new DatabaseRestoreResult(
                source,
                safetyBackupPath);
        }
        catch
        {
            DeleteIfExists(
                tempRestorePath);

            // ถ้า File.Replace สำเร็จไปแล้วแต่ขั้นตรวจภายหลังมีปัญหา
            // rollback file จะยังอยู่ ให้พยายามคืนฐานข้อมูลเดิมทันที
            if (File.Exists(rollbackPath))
            {
                try
                {
                    SqliteConnection.ClearAllPools();

                    File.Copy(
                        rollbackPath,
                        liveDatabase,
                        overwrite: true);

                    DeleteIfExists(
                        liveDatabase + "-wal");

                    DeleteIfExists(
                        liveDatabase + "-shm");
                }
                catch
                {
                    // Safety backup ที่สร้างก่อน Restore ยังอยู่
                    // เพื่อให้ผู้ดูแลสามารถกู้คืนได้ภายหลัง
                }
            }

            throw;
        }
        finally
        {
            DeleteIfExists(
                tempRestorePath);

            DeleteIfExists(
                rollbackPath);
        }
    }

    private static void ValidateDatabaseFile(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "ไม่พบไฟล์ฐานข้อมูล",
                filePath);
        }

        FileInfo info =
            new(filePath);

        if (info.Length <= 0)
        {
            throw new InvalidOperationException(
                "ไฟล์ฐานข้อมูลว่างเปล่า");
        }

        using SqliteConnection connection =
            new(BuildConnectionString(
                filePath,
                SqliteOpenMode.ReadOnly));

        connection.Open();

        using (SqliteCommand integrityCommand =
               connection.CreateCommand())
        {
            integrityCommand.CommandText =
                "PRAGMA quick_check;";

            string result =
                integrityCommand.ExecuteScalar()
                    ?.ToString()
                ?? string.Empty;

            if (!string.Equals(
                    result,
                    "ok",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"ไฟล์ฐานข้อมูลไม่ผ่านการตรวจสอบ SQLite: {result}");
            }
        }

        string[] requiredTables =
        {
            "Customers",
            "PawnTickets",
            "PawnTransactions",
            "AppSettings"
        };

        foreach (string table in requiredTables)
        {
            using SqliteCommand tableCommand =
                connection.CreateCommand();

            tableCommand.CommandText =
                """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = $tableName;
                """;

            tableCommand.Parameters.AddWithValue(
                "$tableName",
                table);

            long exists =
                Convert.ToInt64(
                    tableCommand.ExecuteScalar() ?? 0L);

            if (exists != 1L)
            {
                throw new InvalidOperationException(
                    $"ไฟล์ที่เลือกไม่ใช่ Backup ที่รองรับ: ไม่พบตาราง {table}");
            }
        }
    }

    private static string BuildConnectionString(
        string databaseFile,
        SqliteOpenMode mode)
    {
        SqliteConnectionStringBuilder builder =
            new()
            {
                DataSource = databaseFile,
                Mode = mode
            };

        return builder.ToString();
    }

    private static int ExecuteCount(
        SqliteConnection connection,
        string sql)
    {
        using SqliteCommand command =
            connection.CreateCommand();

        command.CommandText = sql;

        return Convert.ToInt32(
            command.ExecuteScalar() ?? 0);
    }

    private static string? ExecuteString(
        SqliteConnection connection,
        string sql)
    {
        using SqliteCommand command =
            connection.CreateCommand();

        command.CommandText = sql;

        object? value =
            command.ExecuteScalar();

        return value is null ||
               value == DBNull.Value
            ? null
            : value.ToString();
    }

    private static string GetSafetyBackupDirectory()
    {
        string dataDirectory =
            Path.GetDirectoryName(
                DatabasePaths.DatabaseFile)
            ?? throw new InvalidOperationException(
                "ไม่พบตำแหน่งฐานข้อมูล");

        DirectoryInfo? applicationDirectory =
            Directory.GetParent(
                dataDirectory);

        string root =
            applicationDirectory?.FullName
            ?? dataDirectory;

        return Path.Combine(
            root,
            "Backups");
    }

    private static void DeleteIfExists(
        string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
