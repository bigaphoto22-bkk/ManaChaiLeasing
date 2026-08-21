using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ManaChaiLeasing.Services;

public enum ThaiIdReaderStatus
{
    Ready,
    ReaderFoundNoCard,
    NoReader,
    PcScUnavailable,
    Error
}

public sealed record ThaiIdReaderDetectionResult(
    ThaiIdReaderStatus Status,
    string StatusText,
    string? ReaderName,
    int ReaderCount,
    string? TechnicalMessage)
{
    public bool CanAttemptRead =>
        Status == ThaiIdReaderStatus.Ready;
}

public sealed class ThaiIdCardReaderService
{
    private const uint ScardScopeSystem = 0x0002;
    private const uint ScardStateUnaware = 0x00000000;
    private const uint ScardStatePresent = 0x00000020;
    private const int ScardSuccess = 0;
    private const uint ScardENoReadersAvailable = 0x8010002E;

    public ThaiIdReaderDetectionResult Detect()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new ThaiIdReaderDetectionResult(
                ThaiIdReaderStatus.PcScUnavailable,
                "ระบบอ่านบัตรประชาชนรองรับ Windows เท่านั้น",
                null,
                0,
                "Operating system is not Windows.");
        }

        IntPtr context = IntPtr.Zero;

        try
        {
            int result =
                SCardEstablishContext(
                    ScardScopeSystem,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out context);

            if (result != ScardSuccess)
            {
                return new ThaiIdReaderDetectionResult(
                    ThaiIdReaderStatus.PcScUnavailable,
                    "ไม่สามารถเชื่อมต่อบริการ Smart Card ของ Windows ได้",
                    null,
                    0,
                    $"SCardEstablishContext failed: 0x{unchecked((uint)result):X8}");
            }

            List<string> readers =
                GetReaders(
                    context);

            if (readers.Count == 0)
            {
                return new ThaiIdReaderDetectionResult(
                    ThaiIdReaderStatus.NoReader,
                    "ไม่พบเครื่องอ่านบัตรประชาชน",
                    null,
                    0,
                    "PC/SC returned zero smart-card readers.");
            }

            string? firstReaderWithCard = null;

            foreach (string reader in readers)
            {
                if (IsCardPresent(
                        context,
                        reader))
                {
                    firstReaderWithCard =
                        reader;
                    break;
                }
            }

            if (firstReaderWithCard is not null)
            {
                return new ThaiIdReaderDetectionResult(
                    ThaiIdReaderStatus.Ready,
                    "พร้อมอ่านบัตรประชาชน",
                    firstReaderWithCard,
                    readers.Count,
                    null);
            }

            return new ThaiIdReaderDetectionResult(
                ThaiIdReaderStatus.ReaderFoundNoCard,
                readers.Count == 1
                    ? "พบเครื่องอ่านแล้ว • กรุณาเสียบบัตรประชาชน"
                    : $"พบเครื่องอ่าน {readers.Count} เครื่อง • กรุณาเสียบบัตรประชาชน",
                readers[0],
                readers.Count,
                null);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Thai ID reader detection failed.",
                ex);

            return new ThaiIdReaderDetectionResult(
                ThaiIdReaderStatus.Error,
                "ตรวจสอบเครื่องอ่านบัตรไม่สำเร็จ",
                null,
                0,
                ex.Message);
        }
        finally
        {
            if (context != IntPtr.Zero)
            {
                try
                {
                    SCardReleaseContext(
                        context);
                }
                catch
                {
                    // PC/SC cleanup must never block manual customer entry.
                }
            }
        }
    }

    private static List<string> GetReaders(
        IntPtr context)
    {
        uint length = 0;

        int result =
            SCardListReaders(
                context,
                null,
                null,
                ref length);

        if (result != ScardSuccess)
        {
            if (unchecked((uint)result) ==
                ScardENoReadersAvailable)
            {
                return [];
            }

            throw new Win32Exception(
                result,
                $"SCardListReaders(length) failed: 0x{unchecked((uint)result):X8}");
        }

        if (length == 0)
        {
            return [];
        }

        char[] buffer =
            new char[checked((int)length)];

        result =
            SCardListReaders(
                context,
                null,
                buffer,
                ref length);

        if (result != ScardSuccess)
        {
            throw new Win32Exception(
                result,
                $"SCardListReaders(data) failed: 0x{unchecked((uint)result):X8}");
        }

        return new string(buffer)
            .Split(
                '\0',
                StringSplitOptions.RemoveEmptyEntries)
            .Where(
                reader =>
                    !string.IsNullOrWhiteSpace(
                        reader))
            .Distinct(
                StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCardPresent(
        IntPtr context,
        string readerName)
    {
        SCARD_READERSTATE[] states =
        [
            new SCARD_READERSTATE
            {
                szReader = readerName,
                pvUserData = IntPtr.Zero,
                dwCurrentState = ScardStateUnaware,
                dwEventState = 0,
                cbAtr = 0,
                rgbAtr = new byte[36]
            }
        ];

        int result =
            SCardGetStatusChange(
                context,
                0,
                states,
                1);

        if (result != ScardSuccess)
        {
            return false;
        }

        return (
            states[0].dwEventState
            & ScardStatePresent
        ) != 0;
    }

    [StructLayout(
        LayoutKind.Sequential,
        CharSet = CharSet.Unicode)]
    private struct SCARD_READERSTATE
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string szReader;

        public IntPtr pvUserData;
        public uint dwCurrentState;
        public uint dwEventState;
        public uint cbAtr;

        [MarshalAs(
            UnmanagedType.ByValArray,
            SizeConst = 36)]
        public byte[] rgbAtr;
    }

    [DllImport(
        "winscard.dll",
        SetLastError = false)]
    private static extern int SCardEstablishContext(
        uint dwScope,
        IntPtr pvReserved1,
        IntPtr pvReserved2,
        out IntPtr phContext);

    [DllImport(
        "winscard.dll",
        SetLastError = false)]
    private static extern int SCardReleaseContext(
        IntPtr hContext);

    [DllImport(
        "winscard.dll",
        CharSet = CharSet.Unicode,
        SetLastError = false)]
    private static extern int SCardListReaders(
        IntPtr hContext,
        string? mszGroups,
        [Out] char[]? mszReaders,
        ref uint pcchReaders);

    [DllImport(
        "winscard.dll",
        CharSet = CharSet.Unicode,
        SetLastError = false)]
    private static extern int SCardGetStatusChange(
        IntPtr hContext,
        uint dwTimeout,
        [In, Out] SCARD_READERSTATE[] rgReaderStates,
        uint cReaders);
}
