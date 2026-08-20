using System.IO;
using ManaChaiLeasing.Data;
using Microsoft.Data.Sqlite;

namespace ManaChaiLeasing.Services;

public enum DatabaseHealthStatus
{
    Healthy,
    DatabaseMissing,
    Unhealthy
}

public sealed record DatabaseHealthResult(
    DatabaseHealthStatus Status,
    string UserMessage,
    string TechnicalMessage)
{
    public bool IsHealthy =>
        Status == DatabaseHealthStatus.Healthy;

    public bool IsMissing =>
        Status == DatabaseHealthStatus.DatabaseMissing;
}

public sealed class DatabaseHealthService
{
    private static readonly string[] RequiredTables =
    [
        "Customers",
        "PawnTickets",
        "PawnTransactions",
        "AppSettings",
        "SmartLookupValues"
    ];

    public DatabaseHealthResult CheckBeforeInitialization()
    {
        string databaseFile =
            Path.GetFullPath(
                DatabasePaths.DatabaseFile);

        if (!File.Exists(
                databaseFile))
        {
            return new DatabaseHealthResult(
                DatabaseHealthStatus.DatabaseMissing,
                "ยังไม่มีฐานข้อมูล ระบบจะสร้างฐานข้อมูลใหม่",
                "Database file does not exist yet.");
        }

        return CheckDatabase(
            databaseFile,
            requireApplicationTables: false);
    }

    public DatabaseHealthResult CheckAfterInitialization()
    {
        string databaseFile =
            Path.GetFullPath(
                DatabasePaths.DatabaseFile);

        if (!File.Exists(
                databaseFile))
        {
            return new DatabaseHealthResult(
                DatabaseHealthStatus.Unhealthy,
                "ไม่พบไฟล์ฐานข้อมูลหลังเตรียมระบบ",
                "Database file is missing after initialization.");
        }

        return CheckDatabase(
            databaseFile,
            requireApplicationTables: true);
    }

    private static DatabaseHealthResult CheckDatabase(
        string databaseFile,
        bool requireApplicationTables)
    {
        try
        {
            FileInfo info =
                new(databaseFile);

            if (info.Length <= 0)
            {
                return new DatabaseHealthResult(
                    DatabaseHealthStatus.Unhealthy,
                    "ไฟล์ฐานข้อมูลว่างเปล่าหรือไม่สมบูรณ์",
                    "Database file size is zero.");
            }

            SqliteConnectionStringBuilder builder =
                new()
                {
                    DataSource = databaseFile,
                    Mode = SqliteOpenMode.ReadOnly,
                    Pooling = false
                };

            using SqliteConnection connection =
                new(builder.ToString());

            connection.Open();

            using (
                SqliteCommand quickCheck =
                    connection.CreateCommand())
            {
                quickCheck.CommandText =
                    "PRAGMA quick_check;";

                string result =
                    quickCheck.ExecuteScalar()
                        ?.ToString()
                    ?? string.Empty;

                if (!string.Equals(
                        result,
                        "ok",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return new DatabaseHealthResult(
                        DatabaseHealthStatus.Unhealthy,
                        "ฐานข้อมูลไม่ผ่านการตรวจสอบความสมบูรณ์",
                        $"PRAGMA quick_check returned: {result}");
                }
            }

            if (requireApplicationTables)
            {
                foreach (string table in RequiredTables)
                {
                    using SqliteCommand command =
                        connection.CreateCommand();

                    command.CommandText =
                        """
                        SELECT COUNT(*)
                        FROM sqlite_master
                        WHERE type = 'table'
                          AND name = $tableName;
                        """;

                    command.Parameters.AddWithValue(
                        "$tableName",
                        table);

                    long count =
                        Convert.ToInt64(
                            command.ExecuteScalar()
                            ?? 0L);

                    if (count != 1L)
                    {
                        return new DatabaseHealthResult(
                            DatabaseHealthStatus.Unhealthy,
                            "โครงสร้างฐานข้อมูลไม่ครบถ้วน",
                            $"Required table is missing: {table}");
                    }
                }
            }

            return new DatabaseHealthResult(
                DatabaseHealthStatus.Healthy,
                "ฐานข้อมูลพร้อมใช้งาน",
                "Database health check passed.");
        }
        catch (Exception ex)
        {
            return new DatabaseHealthResult(
                DatabaseHealthStatus.Unhealthy,
                "ไม่สามารถเปิดหรือตรวจสอบฐานข้อมูลได้",
                ex.ToString());
        }
    }
}
