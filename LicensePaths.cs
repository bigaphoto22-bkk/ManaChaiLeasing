using System.IO;

namespace ManaChaiLeasing;

public static class LicensePaths
{
    public static string LicenseDirectory =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ManaChaiLeasing",
            "License");

    public static string LicenseFile =>
        Path.Combine(
            LicenseDirectory,
            "ManaChaiLeasing.license");
}
