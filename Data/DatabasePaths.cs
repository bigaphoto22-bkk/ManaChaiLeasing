using System.IO;

namespace ManaChaiLeasing.Data;

public static class DatabasePaths
{
    public static string DataDirectory
    {
        get
        {
            string localAppData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            return Path.Combine(localAppData, "ManaChaiLeasing", "Data");
        }
    }

    public static string DatabaseFile =>
        Path.Combine(DataDirectory, "ManaChaiLeasing.db");

    public static string ConnectionString =>
        $"Data Source={DatabaseFile}";
}
