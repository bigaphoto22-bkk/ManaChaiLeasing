using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace ManaChaiLeasing.Services;

public sealed record MachineIdentity(
    string MachineId,
    string FingerprintVersion);

public sealed class MachineIdentityService
{
    private const string FingerprintVersion = "MID-V1";

    public MachineIdentity GetIdentity()
    {
        string machineGuid =
            ReadWindowsMachineGuid();

        string systemDriveSerial =
            ReadSystemDriveSerial();

        string source =
            $"{FingerprintVersion}|{machineGuid}|{systemDriveSerial}";

        byte[] hash =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(source));

        // ใช้ 6 bytes แรก = 12 hex chars
        // แสดงเป็น MC-XXXX-XXXX-XXXX
        string hex =
            Convert.ToHexString(
                hash.AsSpan(0, 6));

        string machineId =
            $"MC-{hex[..4]}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}";

        return new MachineIdentity(
            machineId,
            FingerprintVersion);
    }

    private static string ReadWindowsMachineGuid()
    {
        try
        {
            using RegistryKey baseKey =
                RegistryKey.OpenBaseKey(
                    RegistryHive.LocalMachine,
                    RegistryView.Registry64);

            using RegistryKey? key =
                baseKey.OpenSubKey(
                    @"SOFTWARE\Microsoft\Cryptography");

            string? value =
                key?.GetValue("MachineGuid")
                    ?.ToString()
                    ?.Trim();

            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        catch
        {
            // ใช้ fallback ด้านล่าง
        }

        // ไม่ควรใช้ค่า raw นี้แสดงต่อผู้ใช้
        // แต่ยังช่วยให้ระบบมี identity ในกรณี Registry อ่านไม่ได้
        return $"FALLBACK-{Environment.MachineName}";
    }

    private static string ReadSystemDriveSerial()
    {
        try
        {
            string rootPath =
                Path.GetPathRoot(
                    Environment.SystemDirectory)
                ?? @"C:\";

            StringBuilder volumeName =
                new(261);

            StringBuilder fileSystemName =
                new(261);

            bool ok =
                GetVolumeInformation(
                    rootPath,
                    volumeName,
                    volumeName.Capacity,
                    out uint serialNumber,
                    out _,
                    out _,
                    fileSystemName,
                    fileSystemName.Capacity);

            if (ok)
            {
                return serialNumber.ToString("X8");
            }
        }
        catch
        {
            // ใช้ fallback ด้านล่าง
        }

        return "NO-VOLUME-SERIAL";
    }

    [DllImport(
        "kernel32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetVolumeInformation(
        string lpRootPathName,
        StringBuilder lpVolumeNameBuffer,
        int nVolumeNameSize,
        out uint lpVolumeSerialNumber,
        out uint lpMaximumComponentLength,
        out uint lpFileSystemFlags,
        StringBuilder lpFileSystemNameBuffer,
        int nFileSystemNameSize);
}
