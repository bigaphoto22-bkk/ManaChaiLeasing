using System.Text;

namespace ManaChaiLeasing.Services;

public sealed record ThaiIdCardRawData(
    byte[] CitizenId,
    byte[] ThaiFullName,
    byte[] EnglishFullName,
    byte[] BirthDate,
    byte[] Gender,
    byte[] CardIssuer,
    byte[] IssueDate,
    byte[] ExpireDate,
    byte[] Address);

public sealed class ThaiIdCardParser
{
    public ThaiIdCardData Parse(
        ThaiIdCardRawData raw,
        ThaiIdCardDataSource source)
    {
        string citizenId =
            DigitsOnly(
                ThaiIdCardTextCodec.Decode(raw.CitizenId));

        (string thaiPrefix,
         string thaiFirstName,
         string thaiLastName) =
            ParseStructuredName(
                ThaiIdCardTextCodec.Decode(raw.ThaiFullName));

        (string englishPrefix,
         string englishFirstName,
         string englishLastName) =
            ParseStructuredName(
                ThaiIdCardTextCodec.Decode(raw.EnglishFullName));

        string genderCode =
            ThaiIdCardTextCodec.Decode(raw.Gender)
                .Trim();

        return new ThaiIdCardData
        {
            Source = source,
            CitizenId = citizenId,
            ThaiPrefix = thaiPrefix,
            ThaiFirstName = thaiFirstName,
            ThaiLastName = thaiLastName,
            EnglishPrefix = englishPrefix,
            EnglishFirstName = englishFirstName,
            EnglishLastName = englishLastName,
            BirthDate = ParseThaiCardDate(raw.BirthDate),
            Gender = ParseGender(genderCode),
            CardIssuer = CleanField(
                ThaiIdCardTextCodec.Decode(raw.CardIssuer)),
            IssueDate = ParseThaiCardDate(raw.IssueDate),
            ExpireDate = ParseThaiCardDate(raw.ExpireDate),
            Address = NormalizeAddress(
                ThaiIdCardTextCodec.Decode(raw.Address))
        };
    }

    private static (
        string Prefix,
        string FirstName,
        string LastName) ParseStructuredName(
            string value)
    {
        string cleaned =
            CleanField(value);

        string[] parts = cleaned
            .Split(
                '#',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries)
            .Where(part =>
                !string.IsNullOrWhiteSpace(part))
            .ToArray();

        if (parts.Length >= 3)
        {
            return (
                parts[0],
                parts[1],
                string.Join(
                    " ",
                    parts.Skip(2)));
        }

        string[] fallback = cleaned
            .Replace('#', ' ')
            .Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);

        return fallback.Length switch
        {
            >= 3 => (
                fallback[0],
                fallback[1],
                string.Join(
                    " ",
                    fallback.Skip(2))),
            2 => (
                string.Empty,
                fallback[0],
                fallback[1]),
            1 => (
                string.Empty,
                fallback[0],
                string.Empty),
            _ => (
                string.Empty,
                string.Empty,
                string.Empty)
        };
    }

    private static DateTime? ParseThaiCardDate(
        byte[] bytes)
    {
        string value =
            DigitsOnly(
                ThaiIdCardTextCodec.Decode(bytes));

        if (value.Length < 8 ||
            !int.TryParse(value[..4], out int year) ||
            !int.TryParse(value.Substring(4, 2), out int month) ||
            !int.TryParse(value.Substring(6, 2), out int day))
        {
            return null;
        }

        // Thai ID cards normally expose Buddhist Era years.
        // Keep a Gregorian DateTime internally for age/calculation safety.
        if (year >= 2400)
        {
            year -= 543;
        }

        try
        {
            return new DateTime(
                year,
                month,
                day);
        }
        catch
        {
            return null;
        }
    }

    private static string ParseGender(
        string value) =>
        value switch
        {
            "1" => "ชาย",
            "2" => "หญิง",
            _ => string.Empty
        };

    private static string NormalizeAddress(
        string value)
    {
        string replaced =
            CleanField(value)
                .Replace('#', ' ');

        return string.Join(
            " ",
            replaced.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries));
    }

    private static string CleanField(
        string value) =>
        value
            .Replace('\0', ' ')
            .Trim('\u0000', '\u00FF', ' ', '#');

    private static string DigitsOnly(
        string value) =>
        new(
            value
                .Where(char.IsDigit)
                .ToArray());
}

internal static class ThaiIdCardTextCodec
{
    internal static string Decode(
        byte[] bytes)
    {
        StringBuilder builder = new();

        foreach (byte value in bytes)
        {
            if (value == 0x00 ||
                value == 0xFF)
            {
                continue;
            }

            if (value <= 0x7F)
            {
                builder.Append((char)value);
                continue;
            }

            if (value is >= 0xA1 and <= 0xDA)
            {
                builder.Append(
                    (char)(0x0E01 +
                           (value - 0xA1)));
                continue;
            }

            if (value == 0xDF)
            {
                builder.Append('\u0E3F');
                continue;
            }

            if (value is >= 0xE0 and <= 0xFB)
            {
                builder.Append(
                    (char)(0x0E40 +
                           (value - 0xE0)));
                continue;
            }

            builder.Append(' ');
        }

        return builder
            .ToString()
            .Trim();
    }

    internal static byte[] EncodeForDevelopmentMock(
        string value,
        int fixedLength)
    {
        List<byte> bytes = [];

        foreach (char character in value)
        {
            if (character <= 0x7F)
            {
                bytes.Add((byte)character);
                continue;
            }

            if (character is >= '\u0E01' and <= '\u0E3A')
            {
                bytes.Add(
                    (byte)(0xA1 +
                           (character - '\u0E01')));
                continue;
            }

            if (character == '\u0E3F')
            {
                bytes.Add(0xDF);
                continue;
            }

            if (character is >= '\u0E40' and <= '\u0E5B')
            {
                bytes.Add(
                    (byte)(0xE0 +
                           (character - '\u0E40')));
                continue;
            }

            bytes.Add((byte)' ');
        }

        if (bytes.Count > fixedLength)
        {
            return bytes
                .Take(fixedLength)
                .ToArray();
        }

        while (bytes.Count < fixedLength)
        {
            bytes.Add((byte)' ');
        }

        return bytes.ToArray();
    }
}
