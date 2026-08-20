namespace ManaChaiLeasing.Licensing;

internal static class VendorPublicKey
{
    public const string KeyId = "MC-KEY-F31B3F5AF1E6";

    private const string PemBase64 = "LS0tLS1CRUdJTiBQVUJMSUMgS0VZLS0tLS0KTUlJQm9qQU5CZ2txaGtpRzl3MEJBUUVGQUFPQ0FZOEFNSUlCaWdLQ0FZRUEySkZkODdialQ3MmZPd1dhVitaRQorUzFxTytZM2Z5STN3TytrNXE3U2E2a3Z1ZzNrbk9yaHNsZ1pXR2VSbG1wWmRJTE5wdHpoYk1IemFpeG1JOUhtCmZUZk1FSTJVRWpiRnR6UG95V2xjWUd4NzlmWjFXaDFzVERCbUVVZi96V0hQM3hjbWlxcXRqVWxYVWsva0kxQ1cKZHRYTG8yaHduLzBNN1BYL2FFdzRwUGZ6OXdWMDh5RHlkQXBoSHFab2tWWDRGKzJTREVXT1lwMFhGcnNLNkFoeApXL25PNkNlN0hVUjRmek5PZmpsalFhSk1DdDI1YWx2ODNpV0NrSDhELzFmeVoxSURCZVR2VjRjTEdXVUJsc1hOClgyLzJENzNPSDZrS2NaSkYyWXloL3NNUnRGWGtWVFhFTWdVbU12b0NEaHIrbXc4UitrbVFoVUNYQThGRzBEV1MKMkJpZVA0ZnJYa1FOK2s1SXFjVzhiRTNyZ2VESmpicmhBU3ZvbDlzR3hQZm84UndSaFo2RkVPYk0xTDV3dHZNTworR3ZaaFZXTlBOaVZkNVR4dzBNWGZ5a0JHYUZvMXdoZkhrWGwvZGQrMzJmRVFGYU9EblRZVW1hNjkrRmZnQUV0CkQrSjVValduSVhnMGJCS3VzeERoWEYwR0NjZWxDMDJBemQybEVXczBHNlI1QWdNQkFBRT0KLS0tLS1FTkQgUFVCTElDIEtFWS0tLS0t";

    public static bool IsConfigured =>
        KeyId != "NOT-CONFIGURED" &&
        !string.IsNullOrWhiteSpace(PemBase64);

    public static string Pem =>
        string.IsNullOrWhiteSpace(PemBase64)
            ? string.Empty
            : System.Text.Encoding.UTF8.GetString(
                Convert.FromBase64String(PemBase64));
}
