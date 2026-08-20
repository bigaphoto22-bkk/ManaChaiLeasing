using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ManaChaiLeasing.Licensing;

namespace ManaChaiLeasing.Services;

public enum LicenseValidationStatus
{
    Valid = 0,
    Missing = 1,
    PublicKeyNotConfigured = 2,
    InvalidFormat = 3,
    InvalidSignature = 4,
    WrongMachine = 5,
    Expired = 6,
    Unsupported = 7,
    Error = 8
}

public sealed class ClientLicensePayload
{
    public string SchemaVersion { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public string LicenseId { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public string MachineId { get; set; } = string.Empty;

    public string LicenseType { get; set; } = string.Empty;

    public DateTime IssuedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

public sealed class ClientSignedLicenseFile
{
    public ClientLicensePayload Payload { get; set; } = new();

    public string SignatureAlgorithm { get; set; } = string.Empty;

    public string SignatureBase64 { get; set; } = string.Empty;
}

public sealed class LicenseValidationResult
{
    public LicenseValidationStatus Status { get; init; }

    public string Message { get; init; } = string.Empty;

    public ClientLicensePayload? Payload { get; init; }

    public bool IsValid =>
        Status == LicenseValidationStatus.Valid;

    public string LicenseTypeText =>
        Payload?.LicenseType switch
        {
            "Trial" => "Trial 7 วัน",
            "Permanent" => "Permanent",
            _ => "-"
        };

    public string ExpiryText
    {
        get
        {
            if (Payload is null)
            {
                return "-";
            }

            if (Payload.LicenseType == "Permanent")
            {
                return "ไม่หมดอายุ";
            }

            if (!Payload.ExpiresAtUtc.HasValue)
            {
                return "-";
            }

            return Payload.ExpiresAtUtc.Value
                .ToLocalTime()
                .ToString("dd/MM/yyyy HH:mm");
        }
    }
}

public sealed class LicenseValidationService
{
    private readonly MachineIdentityService _machineIdentityService = new();

    public LicenseValidationResult ValidateInstalledLicense()
    {
        return ValidateLicenseFile(
            LicensePaths.LicenseFile);
    }

    public LicenseValidationResult ValidateLicenseFile(
        string filePath)
    {
        if (!VendorPublicKey.IsConfigured)
        {
            return Invalid(
                LicenseValidationStatus.PublicKeyNotConfigured,
                "โปรแกรมยังไม่ได้ฝัง Vendor Public Key");
        }

        if (string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            return Invalid(
                LicenseValidationStatus.Missing,
                "ยังไม่พบไฟล์ License สำหรับเครื่องนี้");
        }

        try
        {
            string json =
                File.ReadAllText(
                    filePath,
                    Encoding.UTF8);

            ClientSignedLicenseFile? signedLicense =
                JsonSerializer.Deserialize<ClientSignedLicenseFile>(
                    json);

            if (signedLicense?.Payload is null)
            {
                return Invalid(
                    LicenseValidationStatus.InvalidFormat,
                    "รูปแบบไฟล์ License ไม่ถูกต้อง");
            }

            ClientLicensePayload payload =
                signedLicense.Payload;

            if (!string.Equals(
                    payload.SchemaVersion,
                    "MCL-LIC-1",
                    StringComparison.Ordinal) ||
                !string.Equals(
                    signedLicense.SignatureAlgorithm,
                    "RSA-PSS-SHA256",
                    StringComparison.Ordinal))
            {
                return Invalid(
                    LicenseValidationStatus.Unsupported,
                    "License เวอร์ชันนี้ไม่รองรับ");
            }

            if (!string.Equals(
                    payload.KeyId,
                    VendorPublicKey.KeyId,
                    StringComparison.Ordinal))
            {
                return Invalid(
                    LicenseValidationStatus.InvalidSignature,
                    "License ไม่ได้ออกด้วย Signing Key ที่โปรแกรมนี้อนุญาต");
            }

            if (string.IsNullOrWhiteSpace(
                    signedLicense.SignatureBase64))
            {
                return Invalid(
                    LicenseValidationStatus.InvalidFormat,
                    "License ไม่มี Digital Signature");
            }

            byte[] signature;

            try
            {
                signature =
                    Convert.FromBase64String(
                        signedLicense.SignatureBase64);
            }
            catch (FormatException)
            {
                return Invalid(
                    LicenseValidationStatus.InvalidFormat,
                    "Digital Signature ในไฟล์ License ไม่ถูกต้อง");
            }

            byte[] canonicalBytes =
                Encoding.UTF8.GetBytes(
                    BuildCanonicalPayload(
                        payload));

            using RSA rsa =
                RSA.Create();

            rsa.ImportFromPem(
                VendorPublicKey.Pem);

            bool signatureValid =
                rsa.VerifyData(
                    canonicalBytes,
                    signature,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss);

            if (!signatureValid)
            {
                return Invalid(
                    LicenseValidationStatus.InvalidSignature,
                    "License ถูกแก้ไข หรือไม่ได้ออกโดยผู้จำหน่าย");
            }

            string actualMachineId =
                _machineIdentityService
                    .GetIdentity()
                    .MachineId;

            if (!string.Equals(
                    payload.MachineId,
                    actualMachineId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    LicenseValidationStatus.WrongMachine,
                    "License นี้ไม่ได้รับอนุญาตสำหรับเครื่องนี้",
                    payload);
            }

            if (string.Equals(
                    payload.LicenseType,
                    "Trial",
                    StringComparison.Ordinal))
            {
                if (!payload.ExpiresAtUtc.HasValue)
                {
                    return Invalid(
                        LicenseValidationStatus.InvalidFormat,
                        "Trial License ไม่มีวันหมดอายุ",
                        payload);
                }

                if (DateTime.UtcNow >=
                    payload.ExpiresAtUtc.Value.ToUniversalTime())
                {
                    return Invalid(
                        LicenseValidationStatus.Expired,
                        "ระยะเวลาทดลองใช้งานสิ้นสุดแล้ว",
                        payload);
                }
            }
            else if (string.Equals(
                         payload.LicenseType,
                         "Permanent",
                         StringComparison.Ordinal))
            {
                // Permanent ไม่มีวันหมดอายุ
            }
            else
            {
                return Invalid(
                    LicenseValidationStatus.Unsupported,
                    "ประเภท License นี้ไม่รองรับ",
                    payload);
            }

            return new LicenseValidationResult
            {
                Status =
                    LicenseValidationStatus.Valid,
                Message =
                    "License ถูกต้อง",
                Payload =
                    payload
            };
        }
        catch (JsonException)
        {
            return Invalid(
                LicenseValidationStatus.InvalidFormat,
                "อ่านไฟล์ License ไม่สำเร็จ");
        }
        catch (CryptographicException)
        {
            return Invalid(
                LicenseValidationStatus.InvalidSignature,
                "License หรือ Public Key ไม่ถูกต้อง");
        }
        catch (Exception ex)
        {
            return Invalid(
                LicenseValidationStatus.Error,
                $"ตรวจสอบ License ไม่สำเร็จ: {ex.Message}");
        }
    }

    public LicenseValidationResult InstallLicense(
        string sourceFilePath)
    {
        LicenseValidationResult validation =
            ValidateLicenseFile(
                sourceFilePath);

        if (!validation.IsValid)
        {
            return validation;
        }

        try
        {
            Directory.CreateDirectory(
                LicensePaths.LicenseDirectory);

            string tempPath =
                Path.Combine(
                    LicensePaths.LicenseDirectory,
                    "ManaChaiLeasing.license.tmp");

            File.Copy(
                sourceFilePath,
                tempPath,
                overwrite: true);

            File.Move(
                tempPath,
                LicensePaths.LicenseFile,
                overwrite: true);

            return ValidateInstalledLicense();
        }
        catch (Exception ex)
        {
            return Invalid(
                LicenseValidationStatus.Error,
                $"ไม่สามารถติดตั้ง License ได้: {ex.Message}");
        }
    }

    private static LicenseValidationResult Invalid(
        LicenseValidationStatus status,
        string message,
        ClientLicensePayload? payload = null)
    {
        return new LicenseValidationResult
        {
            Status = status,
            Message = message,
            Payload = payload
        };
    }

    private static string BuildCanonicalPayload(
        ClientLicensePayload payload)
    {
        // ต้องตรงกับ ManaChai License Generator Phase 2L.3 ทุก field
        return string.Join(
            "\n",
            new[]
            {
                $"SchemaVersion={payload.SchemaVersion}",
                $"KeyId={payload.KeyId}",
                $"LicenseId={payload.LicenseId}",
                $"CustomerName={EscapeCanonical(payload.CustomerName)}",
                $"MachineId={payload.MachineId}",
                $"LicenseType={payload.LicenseType}",
                $"IssuedAtUtc={payload.IssuedAtUtc.ToUniversalTime():O}",
                $"ExpiresAtUtc={(payload.ExpiresAtUtc.HasValue ? payload.ExpiresAtUtc.Value.ToUniversalTime().ToString("O") : string.Empty)}"
            });
    }

    private static string EscapeCanonical(
        string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }
}
