namespace ManaChaiLeasing;

public static class AppInfo
{
    public const string StoreName = "มานะชัย ลิสซิ่ง";

    public static string VersionText
    {
        get
        {
            Version? version =
                typeof(AppInfo).Assembly
                    .GetName()
                    .Version;

            if (version is null)
            {
                return "v-";
            }

            return $"v{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public static string ProductVersionText =>
        $"ManaChaiLeasing {VersionText}";

    public const string CustomerLookupWindowTitle =
        "ค้นหาลูกค้าเก่า - " + StoreName;

    public const string InterestRenewalWindowTitle =
        "ต่อดอก - " + StoreName;

    public const string InterestRenewalSuccessWindowTitle =
        "ต่อดอกสำเร็จ - " + StoreName;

    public const string PawnSaveSuccessWindowTitle =
        "บันทึกตั๋วจำนำสำเร็จ - " + StoreName;

    public const string PawnTicketDetailWindowTitle =
        "รายละเอียดตั๋วจำนำ - " + StoreName;

    public const string LicenseActivationWindowTitle =
        "เปิดใช้งานโปรแกรม - " + StoreName;

    public const string RedemptionWindowTitle =
        "ไถ่ถอน - " + StoreName;

    public const string RedemptionSuccessWindowTitle =
        "ไถ่ถอนสำเร็จ - " + StoreName;
}
