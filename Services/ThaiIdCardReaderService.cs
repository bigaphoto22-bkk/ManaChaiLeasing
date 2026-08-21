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
    private const uint ScardShareShared = 0x0002;
    private const uint ScardProtocolT0 = 0x0001;
    private const uint ScardProtocolT1 = 0x0002;
    private const uint ScardLeaveCard = 0x0000;

    private const uint ScardStateUnaware = 0x00000000;
    private const uint ScardStatePresent = 0x00000020;

    private const int ScardSuccess = 0;
    private const uint ScardENoReadersAvailable = 0x8010002E;

    private static readonly byte[] SelectThaiApplication =
    [
        0x00, 0xA4, 0x04, 0x00, 0x08,
        0xA0, 0x00, 0x00, 0x00,
        0x54, 0x48, 0x00, 0x01
    ];

    private static readonly byte[] CommandCitizenId =
        [0x80, 0xB0, 0x00, 0x04, 0x02, 0x00, 0x0D];

    private static readonly byte[] CommandThaiFullName =
        [0x80, 0xB0, 0x00, 0x11, 0x02, 0x00, 0x64];

    private static readonly byte[] CommandEnglishFullName =
        [0x80, 0xB0, 0x00, 0x75, 0x02, 0x00, 0x64];

    private static readonly byte[] CommandBirthDate =
        [0x80, 0xB0, 0x00, 0xD9, 0x02, 0x00, 0x08];

    private static readonly byte[] CommandGender =
        [0x80, 0xB0, 0x00, 0xE1, 0x02, 0x00, 0x01];

    private static readonly byte[] CommandCardIssuer =
        [0x80, 0xB0, 0x00, 0xF6, 0x02, 0x00, 0x64];

    private static readonly byte[] CommandIssueDate =
        [0x80, 0xB0, 0x01, 0x67, 0x02, 0x00, 0x08];

    private static readonly byte[] CommandExpireDate =
        [0x80, 0xB0, 0x01, 0x6F, 0x02, 0x00, 0x08];

    private static readonly byte[] CommandAddress =
        [0x80, 0xB0, 0x15, 0x79, 0x02, 0x00, 0x64];

    private readonly ThaiIdCardParser _parser = new();

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
            int result = SCardEstablishContext(
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

            List<string> readers = GetReaders(context);

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
                if (IsCardPresent(context, reader))
                {
                    firstReaderWithCard = reader;
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
                    SCardReleaseContext(context);
                }
                catch
                {
                    // Reader failure must never block manual customer entry.
                }
            }
        }
    }

    public ThaiIdCardReadResult ReadCard()
    {
        ThaiIdReaderDetectionResult detection = Detect();

        if (!detection.CanAttemptRead ||
            string.IsNullOrWhiteSpace(detection.ReaderName))
        {
            return ThaiIdCardReadResult.Failed(
                detection.StatusText,
                detection.ReaderName,
                detection.TechnicalMessage);
        }

        IntPtr context = IntPtr.Zero;
        IntPtr card = IntPtr.Zero;

        try
        {
            EnsurePcScSuccess(
                SCardEstablishContext(
                    ScardScopeSystem,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    out context),
                "SCardEstablishContext");

            int connectResult = SCardConnect(
                context,
                detection.ReaderName,
                ScardShareShared,
                ScardProtocolT0 | ScardProtocolT1,
                out card,
                out uint activeProtocol);

            EnsurePcScSuccess(
                connectResult,
                "SCardConnect");

            SendSelectApplication(
                card,
                activeProtocol);

            ThaiIdCardRawData raw = new(
                CitizenId: ReadData(
                    card,
                    activeProtocol,
                    CommandCitizenId),
                ThaiFullName: ReadData(
                    card,
                    activeProtocol,
                    CommandThaiFullName),
                EnglishFullName: ReadData(
                    card,
                    activeProtocol,
                    CommandEnglishFullName),
                BirthDate: ReadData(
                    card,
                    activeProtocol,
                    CommandBirthDate),
                Gender: ReadData(
                    card,
                    activeProtocol,
                    CommandGender),
                CardIssuer: ReadData(
                    card,
                    activeProtocol,
                    CommandCardIssuer),
                IssueDate: ReadData(
                    card,
                    activeProtocol,
                    CommandIssueDate),
                ExpireDate: ReadData(
                    card,
                    activeProtocol,
                    CommandExpireDate),
                Address: ReadData(
                    card,
                    activeProtocol,
                    CommandAddress));

            ThaiIdCardData data = _parser.Parse(
                raw,
                ThaiIdCardDataSource.Hardware);

            ValidateMinimumIdentity(data);

            return ThaiIdCardReadResult.Succeeded(
                data,
                detection.ReaderName);
        }
        catch (Exception ex)
        {
            // Never log raw APDU payloads or personal card data.
            AppLog.Error(
                "Thai ID card read failed.",
                ex);

            return ThaiIdCardReadResult.Failed(
                "อ่านข้อมูลบัตรประชาชนไม่สำเร็จ กรุณาถอดบัตร เสียบใหม่ แล้วลองอีกครั้ง",
                detection.ReaderName,
                ex.Message);
        }
        finally
        {
            if (card != IntPtr.Zero)
            {
                try
                {
                    SCardDisconnect(
                        card,
                        ScardLeaveCard);
                }
                catch
                {
                }
            }

            if (context != IntPtr.Zero)
            {
                try
                {
                    SCardReleaseContext(context);
                }
                catch
                {
                }
            }
        }
    }

    private static void ValidateMinimumIdentity(
        ThaiIdCardData data)
    {
        if (data.CitizenId.Length != 13 ||
            !data.CitizenId.All(char.IsDigit))
        {
            throw new InvalidOperationException(
                "Card returned an invalid Citizen ID field.");
        }

        if (string.IsNullOrWhiteSpace(data.ThaiFirstName) ||
            string.IsNullOrWhiteSpace(data.ThaiLastName))
        {
            throw new InvalidOperationException(
                "Card returned an incomplete Thai name field.");
        }
    }

    private static void SendSelectApplication(
        IntPtr card,
        uint protocol)
    {
        ApduResponse response = Transmit(
            card,
            protocol,
            SelectThaiApplication);

        if (response.IsSuccess)
        {
            return;
        }

        // Some Thai ID cards answer SELECT with 61xx (response bytes available).
        if (response.Sw1 == 0x61)
        {
            _ = GetResponse(
                card,
                protocol,
                response.Sw2 == 0
                    ? (byte)0xFF
                    : response.Sw2);
            return;
        }

        throw new InvalidOperationException(
            $"SELECT Thai ID application failed: {response.StatusWord}");
    }

    private static byte[] ReadData(
        IntPtr card,
        uint protocol,
        byte[] command)
    {
        ApduResponse first = Transmit(
            card,
            protocol,
            command);

        if (first.IsSuccess &&
            first.Data.Length > 0)
        {
            return first.Data;
        }

        byte expectedLength = command[^1];

        if (first.Sw1 == 0x61)
        {
            byte responseLength =
                first.Sw2 == 0
                    ? expectedLength
                    : first.Sw2;

            ApduResponse response = GetResponse(
                card,
                protocol,
                responseLength);

            if (!response.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"GET RESPONSE failed: {response.StatusWord}");
            }

            return response.Data;
        }

        if (first.Sw1 == 0x6C)
        {
            byte responseLength =
                first.Sw2 == 0
                    ? expectedLength
                    : first.Sw2;

            ApduResponse response = GetResponse(
                card,
                protocol,
                responseLength);

            if (!response.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Corrected GET RESPONSE failed: {response.StatusWord}");
            }

            return response.Data;
        }

        throw new InvalidOperationException(
            $"Thai ID READ BINARY failed: {first.StatusWord}");
    }

    private static ApduResponse GetResponse(
        IntPtr card,
        uint protocol,
        byte length)
    {
        byte[] command =
            [0x00, 0xC0, 0x00, 0x00, length];

        return Transmit(
            card,
            protocol,
            command);
    }

    private static ApduResponse Transmit(
        IntPtr card,
        uint protocol,
        byte[] command)
    {
        SCARD_IO_REQUEST sendPci = new()
        {
            dwProtocol = protocol,
            cbPciLength =
                checked((uint)Marshal.SizeOf<SCARD_IO_REQUEST>())
        };

        byte[] receiveBuffer = new byte[4096];
        uint receiveLength =
            checked((uint)receiveBuffer.Length);

        int result = SCardTransmit(
            card,
            ref sendPci,
            command,
            checked((uint)command.Length),
            IntPtr.Zero,
            receiveBuffer,
            ref receiveLength);

        EnsurePcScSuccess(
            result,
            "SCardTransmit");

        if (receiveLength < 2)
        {
            throw new InvalidOperationException(
                "Smart card returned an incomplete APDU response.");
        }

        int dataLength =
            checked((int)receiveLength) - 2;

        byte[] data = receiveBuffer
            .Take(dataLength)
            .ToArray();

        byte sw1 = receiveBuffer[dataLength];
        byte sw2 = receiveBuffer[dataLength + 1];

        return new ApduResponse(
            data,
            sw1,
            sw2);
    }

    private static List<string> GetReaders(
        IntPtr context)
    {
        uint length = 0;

        int result = SCardListReaders(
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

        result = SCardListReaders(
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
            .Where(reader =>
                !string.IsNullOrWhiteSpace(reader))
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

        int result = SCardGetStatusChange(
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

    private static void EnsurePcScSuccess(
        int result,
        string operation)
    {
        if (result == ScardSuccess)
        {
            return;
        }

        throw new Win32Exception(
            result,
            $"{operation} failed: 0x{unchecked((uint)result):X8}");
    }

    private sealed record ApduResponse(
        byte[] Data,
        byte Sw1,
        byte Sw2)
    {
        public bool IsSuccess =>
            Sw1 == 0x90 &&
            Sw2 == 0x00;

        public string StatusWord =>
            $"{Sw1:X2}{Sw2:X2}";
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

    [StructLayout(LayoutKind.Sequential)]
    private struct SCARD_IO_REQUEST
    {
        public uint dwProtocol;
        public uint cbPciLength;
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

    [DllImport(
        "winscard.dll",
        CharSet = CharSet.Unicode,
        SetLastError = false)]
    private static extern int SCardConnect(
        IntPtr hContext,
        string szReader,
        uint dwShareMode,
        uint dwPreferredProtocols,
        out IntPtr phCard,
        out uint pdwActiveProtocol);

    [DllImport(
        "winscard.dll",
        SetLastError = false)]
    private static extern int SCardDisconnect(
        IntPtr hCard,
        uint dwDisposition);

    [DllImport(
        "winscard.dll",
        SetLastError = false)]
    private static extern int SCardTransmit(
        IntPtr hCard,
        ref SCARD_IO_REQUEST pioSendPci,
        byte[] pbSendBuffer,
        uint cbSendLength,
        IntPtr pioRecvPci,
        [Out] byte[] pbRecvBuffer,
        ref uint pcbRecvLength);
}
