namespace ManaChaiLeasing.Services;

public sealed class ThaiIdCardDevelopmentMockService
{
    private readonly ThaiIdCardParser _parser = new();

    public ThaiIdCardData CreateParsedMockData()
    {
        ThaiIdCardRawData raw = new(
            CitizenId: Encode(
                "0000000000000",
                13),
            ThaiFullName: Encode(
                "นาย#มานะ##ทดสอบระบบ#",
                100),
            EnglishFullName: Encode(
                "Mr.#Mana##Test#",
                100),
            BirthDate: Encode(
                "25300115",
                8),
            Gender: Encode(
                "1",
                1),
            CardIssuer: Encode(
                "สำนักทะเบียนทดสอบ",
                100),
            IssueDate: Encode(
                "25660101",
                8),
            ExpireDate: Encode(
                "25730101",
                8),
            Address: Encode(
                "99#หมู่ที่ 1#ถนนทดสอบ#ตำบลทดสอบ#อำเภอทดสอบ#จังหวัดทดสอบ",
                100));

        return _parser.Parse(
            raw,
            ThaiIdCardDataSource.DevelopmentMock);
    }

    private static byte[] Encode(
        string value,
        int fixedLength) =>
        ThaiIdCardTextCodec.EncodeForDevelopmentMock(
            value,
            fixedLength);
}
