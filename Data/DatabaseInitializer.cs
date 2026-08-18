using System.IO;

using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Data;

public static class DatabaseInitializer
{
    public static string Initialize()
    {
        Directory.CreateDirectory(DatabasePaths.DataDirectory);

        using AppDbContext db = new();
        db.Database.Migrate();

        return DatabasePaths.DatabaseFile;
    }
}
