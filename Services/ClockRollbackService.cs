using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;

namespace ManaChaiLeasing.Services;

public enum ClockValidationStatus
{
    Valid = 0,
    RollbackDetected = 1,
    StateCorrupted = 2,
    Error = 3
}

public sealed class ClockValidationResult
{
    public ClockValidationStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsValid =>
        Status == ClockValidationStatus.Valid;
}

internal sealed class ClockState
{
    public string SchemaVersion { get; set; } = "MCL-CLOCK-1";

    public string LicenseId { get; set; } = string.Empty;

    public DateTime LastSeenUtc { get; set; }
}

public sealed class ClockRollbackService
{
    // ยอมให้เวลาคลาดเคลื่อนเล็กน้อย เพื่อไม่ให้ล็อกจากการ sync เวลา Windows
    private static readonly TimeSpan RollbackTolerance =
        TimeSpan.FromMinutes(10);

    private const string RegistryPath =
        @"Software\ManaChaiLeasing\License";

    private const string RegistryValueName =
        "ClockState";

    private static string LocalStateFile =>
        Path.Combine(
            LicensePaths.LicenseDirectory,
            "clock-state.dat");

    private static string RoamingStateFile =>
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData),
            "ManaChaiLeasing",
            "license-clock.dat");

    public ClockValidationResult ValidateAndUpdate(
        string licenseId,
        DateTime issuedAtUtc)
    {
        try
        {
            DateTime nowUtc =
                DateTime.UtcNow;

            DateTime normalizedIssuedUtc =
                issuedAtUtc.ToUniversalTime();

            // License ออกใน "อนาคต" เมื่อเทียบกับนาฬิกาเครื่องอย่างมีนัยสำคัญ
            // แปลว่าวัน/เวลาของเครื่องอาจถูกย้อนก่อนเริ่มใช้งานครั้งแรก
            if (nowUtc + RollbackTolerance <
                normalizedIssuedUtc)
            {
                return Invalid(
                    ClockValidationStatus.RollbackDetected,
                    "ตรวจพบวันที่/เวลาของเครื่องย้อนกลับก่อนวันที่ออก License");
            }

            ClockStateReadResult local =
                ReadProtectedFile(
                    LocalStateFile);

            ClockStateReadResult roaming =
                ReadProtectedFile(
                    RoamingStateFile);

            ClockStateReadResult registry =
                ReadRegistryState();

            List<ClockState> matchingStates =
                new();

            AddMatchingState(
                matchingStates,
                local,
                licenseId);

            AddMatchingState(
                matchingStates,
                roaming,
                licenseId);

            AddMatchingState(
                matchingStates,
                registry,
                licenseId);

            bool hasCorruptedMatchingStorage =
                local.IsCorrupted ||
                roaming.IsCorrupted ||
                registry.IsCorrupted;

            // ถ้ามีอย่างน้อยหนึ่ง state ที่ถูกต้อง ให้ใช้ตัวที่ "ใหม่ที่สุด"
            // แล้วซ่อมสำเนาอื่นกลับให้ตรงกัน
            if (matchingStates.Count > 0)
            {
                DateTime lastSeenUtc =
                    matchingStates
                        .Max(state =>
                            state.LastSeenUtc
                                .ToUniversalTime());

                if (nowUtc + RollbackTolerance <
                    lastSeenUtc)
                {
                    return Invalid(
                        ClockValidationStatus.RollbackDetected,
                        "ตรวจพบการย้อนวันที่/เวลา Windows");
                }

                DateTime trustedUtc =
                    nowUtc > lastSeenUtc
                        ? nowUtc
                        : lastSeenUtc;

                WriteAllStates(
                    new ClockState
                    {
                        LicenseId = licenseId,
                        LastSeenUtc = trustedUtc
                    });

                return Valid();
            }

            // มี state แต่ถอดรหัส/อ่านไม่ได้ทั้งหมด:
            // สำหรับ Trial ให้ถือว่าน่าสงสัย ไม่สร้างใหม่ทับหลักฐานเดิม
            if (hasCorruptedMatchingStorage)
            {
                return Invalid(
                    ClockValidationStatus.StateCorrupted,
                    "ข้อมูลตรวจสอบเวลาเสียหายหรือถูกแก้ไข");
            }

            // First run ของ LicenseId ใหม่:
            // เริ่มต้น state ใหม่ได้ เพราะ LicenseId มาจากไฟล์ที่มี Signature ถูกต้อง
            DateTime initialTrustedUtc =
                nowUtc > normalizedIssuedUtc
                    ? nowUtc
                    : normalizedIssuedUtc;

            WriteAllStates(
                new ClockState
                {
                    LicenseId = licenseId,
                    LastSeenUtc = initialTrustedUtc
                });

            return Valid();
        }
        catch (Exception ex)
        {
            return Invalid(
                ClockValidationStatus.Error,
                $"ไม่สามารถตรวจสอบวันที่/เวลาได้: {ex.Message}");
        }
    }

    private static void AddMatchingState(
        List<ClockState> list,
        ClockStateReadResult result,
        string licenseId)
    {
        if (result.State is null)
        {
            return;
        }

        if (!string.Equals(
                result.State.SchemaVersion,
                "MCL-CLOCK-1",
                StringComparison.Ordinal) ||
            !string.Equals(
                result.State.LicenseId,
                licenseId,
                StringComparison.Ordinal))
        {
            return;
        }

        list.Add(
            result.State);
    }

    private static void WriteAllStates(
        ClockState state)
    {
        byte[] plainBytes =
            JsonSerializer.SerializeToUtf8Bytes(
                state);

        byte[] protectedBytes =
            Dpapi.Protect(
                plainBytes);

        WriteProtectedFile(
            LocalStateFile,
            protectedBytes);

        WriteProtectedFile(
            RoamingStateFile,
            protectedBytes);

        using RegistryKey key =
            Registry.CurrentUser.CreateSubKey(
                RegistryPath,
                writable: true);

        key.SetValue(
            RegistryValueName,
            protectedBytes,
            RegistryValueKind.Binary);
    }

    private static ClockStateReadResult ReadProtectedFile(
        string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return ClockStateReadResult.Missing();
            }

            byte[] protectedBytes =
                File.ReadAllBytes(path);

            return DecodeState(
                protectedBytes);
        }
        catch
        {
            return ClockStateReadResult.Corrupted();
        }
    }

    private static ClockStateReadResult ReadRegistryState()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(
                    RegistryPath,
                    writable: false);

            object? value =
                key?.GetValue(
                    RegistryValueName);

            if (value is not byte[] bytes ||
                bytes.Length == 0)
            {
                return ClockStateReadResult.Missing();
            }

            return DecodeState(
                bytes);
        }
        catch
        {
            return ClockStateReadResult.Corrupted();
        }
    }

    private static ClockStateReadResult DecodeState(
        byte[] protectedBytes)
    {
        try
        {
            byte[] plainBytes =
                Dpapi.Unprotect(
                    protectedBytes);

            ClockState? state =
                JsonSerializer.Deserialize<ClockState>(
                    plainBytes);

            if (state is null ||
                string.IsNullOrWhiteSpace(
                    state.LicenseId) ||
                state.LastSeenUtc == default)
            {
                return ClockStateReadResult.Corrupted();
            }

            return ClockStateReadResult.Valid(
                state);
        }
        catch
        {
            return ClockStateReadResult.Corrupted();
        }
    }

    private static void WriteProtectedFile(
        string path,
        byte[] bytes)
    {
        string? directory =
            Path.GetDirectoryName(
                path);

        if (!string.IsNullOrWhiteSpace(
                directory))
        {
            Directory.CreateDirectory(
                directory);
        }

        string tempPath =
            path + ".tmp";

        File.WriteAllBytes(
            tempPath,
            bytes);

        File.Move(
            tempPath,
            path,
            overwrite: true);
    }

    private static ClockValidationResult Valid()
    {
        return new ClockValidationResult
        {
            Status =
                ClockValidationStatus.Valid,
            Message =
                "วันที่/เวลาถูกต้อง"
        };
    }

    private static ClockValidationResult Invalid(
        ClockValidationStatus status,
        string message)
    {
        return new ClockValidationResult
        {
            Status = status,
            Message = message
        };
    }
}

internal sealed class ClockStateReadResult
{
    public ClockState? State { get; private init; }

    public bool IsCorrupted { get; private init; }

    public static ClockStateReadResult Missing()
    {
        return new ClockStateReadResult();
    }

    public static ClockStateReadResult Valid(
        ClockState state)
    {
        return new ClockStateReadResult
        {
            State = state
        };
    }

    public static ClockStateReadResult Corrupted()
    {
        return new ClockStateReadResult
        {
            IsCorrupted = true
        };
    }
}

internal static class Dpapi
{
    private const uint CryptProtectUiForbidden =
        0x1;

    public static byte[] Protect(
        byte[] plainBytes)
    {
        DataBlob input =
            DataBlob.FromBytes(
                plainBytes);

        DataBlob output =
            default;

        try
        {
            bool ok =
                CryptProtectData(
                    ref input,
                    null,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out output);

            if (!ok)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            return output.ToBytes();
        }
        finally
        {
            input.FreeInput();
            output.FreeOutput();
        }
    }

    public static byte[] Unprotect(
        byte[] protectedBytes)
    {
        DataBlob input =
            DataBlob.FromBytes(
                protectedBytes);

        DataBlob output =
            default;

        try
        {
            bool ok =
                CryptUnprotectData(
                    ref input,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out output);

            if (!ok)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            return output.ToBytes();
        }
        finally
        {
            input.FreeInput();
            output.FreeOutput();
        }
    }

    [StructLayout(
        LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;

        public IntPtr Data;

        public static DataBlob FromBytes(
            byte[] bytes)
        {
            DataBlob blob =
                new()
                {
                    Size = bytes.Length,
                    Data =
                        Marshal.AllocHGlobal(
                            bytes.Length)
                };

            Marshal.Copy(
                bytes,
                0,
                blob.Data,
                bytes.Length);

            return blob;
        }

        public byte[] ToBytes()
        {
            if (Data == IntPtr.Zero ||
                Size <= 0)
            {
                return Array.Empty<byte>();
            }

            byte[] bytes =
                new byte[Size];

            Marshal.Copy(
                Data,
                bytes,
                0,
                Size);

            return bytes;
        }

        public void FreeInput()
        {
            if (Data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(
                    Data);

                Data = IntPtr.Zero;
                Size = 0;
            }
        }

        public void FreeOutput()
        {
            if (Data != IntPtr.Zero)
            {
                LocalFree(
                    Data);

                Data = IntPtr.Zero;
                Size = 0;
            }
        }
    }

    [DllImport(
        "crypt32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string? szDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        out DataBlob pDataOut);

    [DllImport(
        "crypt32.dll",
        CharSet = CharSet.Unicode,
        SetLastError = true)]
    [return: MarshalAs(
        UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        IntPtr ppszDataDescr,
        IntPtr pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        out DataBlob pDataOut);

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    private static extern IntPtr LocalFree(
        IntPtr hMem);
}
