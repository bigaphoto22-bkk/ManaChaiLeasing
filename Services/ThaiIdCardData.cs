namespace ManaChaiLeasing.Services;

public enum ThaiIdCardDataSource
{
    Hardware,
    DevelopmentMock
}

public sealed record ThaiIdCardData
{
    public ThaiIdCardDataSource Source { get; init; }

    public string CitizenId { get; init; } = string.Empty;

    public string ThaiPrefix { get; init; } = string.Empty;

    public string ThaiFirstName { get; init; } = string.Empty;

    public string ThaiLastName { get; init; } = string.Empty;

    public string EnglishPrefix { get; init; } = string.Empty;

    public string EnglishFirstName { get; init; } = string.Empty;

    public string EnglishLastName { get; init; } = string.Empty;

    public DateTime? BirthDate { get; init; }

    public string Gender { get; init; } = string.Empty;

    public string CardIssuer { get; init; } = string.Empty;

    public DateTime? IssueDate { get; init; }

    public DateTime? ExpireDate { get; init; }

    public string Address { get; init; } = string.Empty;

    public int? CalculateAge(DateTime asOfDate)
    {
        if (!BirthDate.HasValue)
        {
            return null;
        }

        DateTime birthDate = BirthDate.Value.Date;
        DateTime date = asOfDate.Date;

        int age = date.Year - birthDate.Year;

        if (birthDate > date.AddYears(-age))
        {
            age--;
        }

        return age is >= 0 and <= 120
            ? age
            : null;
    }
}

public sealed record ThaiIdCardReadResult(
    bool Success,
    string UserMessage,
    ThaiIdCardData? Data,
    string? ReaderName,
    string? TechnicalMessage)
{
    public static ThaiIdCardReadResult Failed(
        string userMessage,
        string? readerName,
        string? technicalMessage) =>
        new(
            false,
            userMessage,
            null,
            readerName,
            technicalMessage);

    public static ThaiIdCardReadResult Succeeded(
        ThaiIdCardData data,
        string readerName) =>
        new(
            true,
            "อ่านข้อมูลบัตรประชาชนสำเร็จ",
            data,
            readerName,
            null);
}
