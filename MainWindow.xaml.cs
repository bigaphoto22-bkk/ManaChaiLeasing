using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class MainWindow : Window
{
    private enum HomeDashboardPeriodPreset
    {
        Today,
        ThisWeek,
        ThisMonth,
        Custom
    }

    private bool _isInitializing = true;
    private bool _isSavingPawnTicket;
    private bool _thaiIdMockDataLoaded;
    private ThaiIdCardData? _pendingThaiIdCardData;
    private readonly CustomerService _customerService = new();
    private readonly PawnTicketService _pawnTicketService = new();
    private readonly PawnTicketSearchService _pawnTicketSearchService = new();
    private readonly AppSettingService _appSettingService = new();
    private readonly TodaySummaryService _todaySummaryService = new();
    private readonly HomeDashboardService _homeDashboardService = new();
    private readonly DatabaseBackupService _databaseBackupService = new();
    private readonly DatabaseHealthService _databaseHealthService = new();
    private readonly AutomaticBackupService _automaticBackupService = new();
    private readonly SupportDiagnosticsService _supportDiagnosticsService = new();
    private readonly ThaiIdCardReaderService _thaiIdCardReaderService = new();
    private readonly ThaiIdCardDevelopmentMockService _thaiIdDevelopmentMockService = new();
    private readonly MachineIdentityService _machineIdentityService = new();
    private readonly LicenseValidationService _licenseValidationService = new();
    private int? _selectedCustomerId;
    private HomeDashboardPeriodPreset _homeDashboardPeriod =
        HomeDashboardPeriodPreset.Today;

    public MainWindow()
    {
        if (!SingleInstanceService.TryAcquire())
        {
            Application.Current.Shutdown();
            return;
        }

        ApplicationDiagnosticsService.Initialize();

        InitializeComponent();
        ConfigureThaiIdDevelopmentTools();

        SingleInstanceService.StartActivationListener(
            BringExistingInstanceToFront);

        // ซ่อน Main Window ไว้ก่อนจนกว่า License จะผ่าน
        Opacity = 0;
        ShowInTaskbar = false;

        if (!EnsureValidLicenseForStartup())
        {
            Application.Current.Shutdown();
            return;
        }

        if (!EnsureHealthyDatabaseForStartup())
        {
            Application.Current.Shutdown();
            return;
        }

        Opacity = 1;
        ShowInTaskbar = true;

        RunAutomaticBackupAfterStartup();

        HomeCustomStartDatePicker.SelectedDate = DateTime.Today;
        HomeCustomEndDatePicker.SelectedDate = DateTime.Today;
        HomeCustomStartDatePicker.DisplayDateEnd = DateTime.Today;
        HomeCustomEndDatePicker.DisplayDateEnd = DateTime.Today;

        UpdateHomeDashboardPeriodButtons();
        LoadHomeDashboard();
        LoadSmartLookupValues();

        PawnDatePicker.SelectedDate = DateTime.Today;

        _isInitializing = false;
        UpdateProductForm();
        UpdateAssetPreview();
    }

    private void BringExistingInstanceToFront()
    {
        try
        {
            Dispatcher.Invoke(
                () =>
                {
                    Window? targetWindow =
                        null;

                    foreach (Window window in
                             Application.Current.Windows)
                    {
                        if (window.IsActive)
                        {
                            targetWindow =
                                window;

                            break;
                        }

                        if (targetWindow is null &&
                            window.IsVisible)
                        {
                            targetWindow =
                                window;
                        }
                    }

                    targetWindow ??=
                        this;

                    if (targetWindow.WindowState ==
                        WindowState.Minimized)
                    {
                        targetWindow.WindowState =
                            WindowState.Normal;
                    }

                    if (!targetWindow.IsVisible)
                    {
                        return;
                    }

                    targetWindow.Show();
                    targetWindow.Activate();

                    // Windows บางครั้งไม่ยอมย้าย focus ข้าม process
                    // Topmost ชั่วคราวช่วยดึง Window เดิมขึ้นหน้า
                    // แล้วคืนค่าทันทีเพื่อไม่ให้โปรแกรมค้าง Always-on-top
                    bool originalTopmost =
                        targetWindow.Topmost;

                    targetWindow.Topmost = true;
                    targetWindow.Topmost =
                        originalTopmost;

                    targetWindow.Focus();

                    AppLog.Info(
                        "Existing application instance brought to foreground.");
                });
        }
        catch (Exception ex)
        {
            AppLog.Warning(
                $"Could not bring existing application instance to foreground: {ex.Message}");
        }
    }

    private bool EnsureValidLicenseForStartup()
    {
        LicenseValidationResult result =
            _licenseValidationService
                .ValidateInstalledLicense();

        if (result.IsValid)
        {
            return true;
        }

        LicenseActivationWindow activationWindow =
            new(result);

        bool? activated =
            activationWindow.ShowDialog();

        if (activated != true)
        {
            return false;
        }

        return _licenseValidationService
            .ValidateInstalledLicense()
            .IsValid;
    }

    private bool EnsureHealthyDatabaseForStartup()
    {
        DatabaseHealthResult preCheck =
            _databaseHealthService
                .CheckBeforeInitialization();

        if (!preCheck.IsHealthy &&
            !preCheck.IsMissing)
        {
            return ShowDatabaseRecovery(
                preCheck);
        }

        try
        {
            DatabaseInitializer.Initialize();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Database initialization failed.",
                ex);

            DatabaseHealthResult initializationFailure =
                new(
                    DatabaseHealthStatus.Unhealthy,
                    "ไม่สามารถเตรียมฐานข้อมูลให้พร้อมใช้งานได้",
                    ex.ToString());

            return ShowDatabaseRecovery(
                initializationFailure);
        }

        DatabaseHealthResult postCheck =
            _databaseHealthService
                .CheckAfterInitialization();

        if (!postCheck.IsHealthy)
        {
            return ShowDatabaseRecovery(
                postCheck);
        }

        DatabaseStatusText.Text =
            "Offline • SQLite Ready";

        DatabaseStatusText.Foreground =
            Brushes.ForestGreen;

        DatabaseStatusText.ToolTip =
            DatabasePaths.DatabaseFile;

        AppLog.Info(
            "Database startup health check passed.");

        return true;
    }

    private bool ShowDatabaseRecovery(
        DatabaseHealthResult healthResult)
    {
        DatabaseStatusText.Text =
            "Database Recovery Required";

        DatabaseStatusText.Foreground =
            Brushes.Firebrick;

        AppLog.Critical(
            $"Database startup health check failed: {healthResult.TechnicalMessage}");

        AppLog.Info(
            "Opening database recovery window.");

        DatabaseRecoveryWindow recoveryWindow =
            new(
                healthResult)
            {
                Topmost = true
            };

        recoveryWindow.Loaded +=
            (_, _) =>
            {
                recoveryWindow.Activate();
                recoveryWindow.Focus();

                // Topmost ใช้เฉพาะตอน Startup เพื่อให้ Recovery
                // ไม่หลบอยู่หลัง Visual Studio / หน้าต่างอื่น
                recoveryWindow.Topmost = false;
            };

        bool? result =
            recoveryWindow.ShowDialog();

        // หลัง Restore ต้องปิดและเปิดโปรแกรมใหม่เสมอ
        // เพื่อให้ EF/SQLite ทุก service เริ่มจากฐานข้อมูลชุดใหม่อย่างสะอาด
        if (result == true &&
            recoveryWindow.RecoveryCompleted)
        {
            return false;
        }

        return false;
    }


    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            HomeContent,
            HomeButton,
            "หน้าหลัก",
            "ภาพรวมตั๋วและเงินเข้าออกของร้าน");

        LoadHomeDashboard();
    }

    private void RefreshHomeDashboardButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LoadHomeDashboard();
    }

    private void HomeDashboardTodayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetHomeDashboardPeriod(
            HomeDashboardPeriodPreset.Today);
    }

    private void HomeDashboardWeekButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetHomeDashboardPeriod(
            HomeDashboardPeriodPreset.ThisWeek);
    }

    private void HomeDashboardMonthButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SetHomeDashboardPeriod(
            HomeDashboardPeriodPreset.ThisMonth);
    }

    private void HomeDashboardCustomButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        _homeDashboardPeriod =
            HomeDashboardPeriodPreset.Custom;

        HomeDashboardCustomRangePanel.Visibility =
            Visibility.Visible;

        HomeCustomStartDatePicker.SelectedDate ??=
            DateTime.Today;

        HomeCustomEndDatePicker.SelectedDate ??=
            DateTime.Today;

        UpdateHomeDashboardPeriodButtons();
        LoadHomeDashboard();
    }

    private void HomeDashboardApplyCustomButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LoadHomeDashboard(showRangeValidation: true);
    }

    private void SetHomeDashboardPeriod(
        HomeDashboardPeriodPreset period)
    {
        _homeDashboardPeriod = period;

        HomeDashboardCustomRangePanel.Visibility =
            period == HomeDashboardPeriodPreset.Custom
                ? Visibility.Visible
                : Visibility.Collapsed;

        UpdateHomeDashboardPeriodButtons();
        LoadHomeDashboard();
    }

    private void UpdateHomeDashboardPeriodButtons()
    {
        Style primary =
            (Style)FindResource("PrimaryButtonStyle");

        Style secondary =
            (Style)FindResource("SecondaryButtonStyle");

        HomeDashboardTodayButton.Style =
            _homeDashboardPeriod ==
                HomeDashboardPeriodPreset.Today
                ? primary
                : secondary;

        HomeDashboardWeekButton.Style =
            _homeDashboardPeriod ==
                HomeDashboardPeriodPreset.ThisWeek
                ? primary
                : secondary;

        HomeDashboardMonthButton.Style =
            _homeDashboardPeriod ==
                HomeDashboardPeriodPreset.ThisMonth
                ? primary
                : secondary;

        HomeDashboardCustomButton.Style =
            _homeDashboardPeriod ==
                HomeDashboardPeriodPreset.Custom
                ? primary
                : secondary;
    }

    private void LoadHomeDashboard(
        bool showRangeValidation = false)
    {
        try
        {
            if (!TryGetHomeDashboardRange(
                out DateTime startDate,
                out DateTime endDate,
                out string validationMessage))
            {
                HomeDashboardUpdatedText.Text =
                    "กรุณาตรวจสอบช่วงวันที่";

                if (showRangeValidation)
                {
                    MessageBox.Show(
                        validationMessage,
                        ManaChaiLeasing.AppInfo.StoreName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                return;
            }

            HomeDashboardSummary summary =
                _homeDashboardService.GetSummary(
                    startDate,
                    endDate);

            HomeDashboardPeriodText.Text =
                summary.StartDate.Date == summary.EndDate.Date
                    ? $"วันที่ {summary.StartDate:dd/MM/yyyy}"
                    : $"ช่วง {summary.StartDate:dd/MM/yyyy} - {summary.EndDate:dd/MM/yyyy}";

            HomeDashboardUpdatedText.Text =
                $"อัปเดตล่าสุด {summary.UpdatedAt:HH:mm}";

            HomeActiveTicketCountText.Text =
                $"{summary.ActiveTicketCount:N0} ตั๋ว";

            HomeDueAtEndCountText.Text =
                $"{summary.DueAtEndDateCount:N0} ตั๋ว";

            HomeOverdueCountText.Text =
                $"{summary.OverdueCount:N0} ตั๋ว";

            HomePawnCountText.Text =
                $"{summary.PawnCount:N0} รายการ";

            HomePawnExpenseText.Text =
                $"จ่ายออก {summary.PawnExpense:N2} บาท";

            HomeInterestCountText.Text =
                $"{summary.InterestCount:N0} ครั้ง";

            HomeInterestIncomeText.Text =
                $"รับเข้า {summary.InterestIncome:N2} บาท";

            HomeRedemptionCountText.Text =
                $"{summary.RedemptionCount:N0} รายการ";

            HomeRedemptionIncomeText.Text =
                $"รับเข้า {summary.RedemptionIncome:N2} บาท";

            HomeTotalIncomeText.Text =
                $"{summary.TotalIncome:N2} บาท";

            HomeTotalExpenseText.Text =
                $"{summary.TotalExpense:N2} บาท";

            HomeNetCashText.Text =
                $"{summary.NetCash:N2} บาท";

            HomeNetCashText.Foreground =
                summary.NetCash < 0m
                    ? Brushes.Firebrick
                    : Brushes.ForestGreen;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            HomeDashboardUpdatedText.Text =
                "ไม่สามารถโหลด Dashboard ได้";

            MessageBox.Show(
                $"ไม่สามารถโหลดข้อมูลหน้าหลักได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool TryGetHomeDashboardRange(
        out DateTime startDate,
        out DateTime endDate,
        out string validationMessage)
    {
        DateTime today = DateTime.Today;

        startDate = today;
        endDate = today;
        validationMessage = string.Empty;

        switch (_homeDashboardPeriod)
        {
            case HomeDashboardPeriodPreset.Today:
                return true;

            case HomeDashboardPeriodPreset.ThisWeek:
                int daysSinceMonday =
                    ((int)today.DayOfWeek + 6) % 7;

                startDate = today.AddDays(-daysSinceMonday);
                return true;

            case HomeDashboardPeriodPreset.ThisMonth:
                startDate = new DateTime(
                    today.Year,
                    today.Month,
                    1);

                return true;

            case HomeDashboardPeriodPreset.Custom:
                if (!HomeCustomStartDatePicker.SelectedDate.HasValue ||
                    !HomeCustomEndDatePicker.SelectedDate.HasValue)
                {
                    validationMessage =
                        "กรุณาเลือกวันที่เริ่มต้นและวันที่สิ้นสุดให้ครบ";
                    return false;
                }

                startDate =
                    HomeCustomStartDatePicker.SelectedDate.Value.Date;

                endDate =
                    HomeCustomEndDatePicker.SelectedDate.Value.Date;

                if (startDate > endDate)
                {
                    validationMessage =
                        "วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด";
                    return false;
                }

                if (endDate > today)
                {
                    validationMessage =
                        "วันที่สิ้นสุดต้องไม่เกินวันนี้";
                    return false;
                }

                return true;

            default:
                return true;
        }
    }

    private void NewPawnButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            NewPawnContent,
            NewPawnButton,
            "รับจำนำใหม่",
            "บันทึกข้อมูลลูกค้า สินค้า หมายเลขตั๋ว และยอดเงิน");
            RefreshThaiIdReaderStatus();
}

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            SearchContent,
            SearchButton,
            "ค้นหารายการ",
            "ค้นหาและเปิดดูข้อมูลตั๋วจำนำย้อนหลัง");

        LoadPawnTicketSearchResults();
        PawnTicketSearchTextBox.Focus();
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            TodayContent,
            TodayButton,
            "รายการวันนี้",
            "สรุปรายการรับจำนำ ต่อดอก ไถ่ถอน และเงินเข้าออกประจำวัน");

        LoadTodaySummary();
    }

    private void RefreshTodayButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        LoadTodaySummary();
    }

    private void LoadTodaySummary()
    {
        try
        {
            TodaySummary summary =
                _todaySummaryService.GetTodaySummary();

            TodayDateText.Text =
                $"วันที่ {summary.Date:dd/MM/yyyy}";

            TodayPawnExpenseText.Text =
                $"{summary.PawnExpense:N2} บาท";
            TodayPawnCountText.Text =
                $"จำนำ {summary.PawnCount:N0} รายการ";

            TodayInterestIncomeText.Text =
                $"{summary.InterestIncome:N2} บาท";
            TodayInterestCountText.Text =
                $"ต่อดอก {summary.InterestCount:N0} รายการ";

            TodayRedemptionIncomeText.Text =
                $"{summary.RedemptionIncome:N2} บาท";
            TodayRedemptionCountText.Text =
                $"ไถ่ถอน {summary.RedemptionCount:N0} รายการ";

            TodayNetCashText.Text =
                $"{summary.NetCash:N2} บาท";
            TodayNetCashText.Foreground =
                summary.NetCash < 0m
                    ? Brushes.Firebrick
                    : Brushes.ForestGreen;

            TodayTotalIncomeText.Text =
                $"รับเข้ารวม {summary.TotalIncome:N2} บาท";

            TodayTransactionCountText.Text =
                summary.TransactionCount == 0
                    ? "ยังไม่มีรายการวันนี้"
                    : $"ทั้งหมด {summary.TransactionCount:N0} รายการ";

            TodayTransactionsDataGrid.ItemsSource =
                summary.Transactions;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            TodayTransactionsDataGrid.ItemsSource = null;
            TodayTransactionCountText.Text =
                "ไม่สามารถโหลดข้อมูลได้";

            MessageBox.Show(
                $"ไม่สามารถโหลดรายการวันนี้ได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            SettingsContent,
            SettingsButton,
            "ตั้งค่า",
            "ตั้งค่าข้อมูลร้าน เงื่อนไข และการสำรองข้อมูล");

        DatabaseFilePathText.Text =
            DatabasePaths.DatabaseFile;

        LoadMachineIdentity();
        LoadLicenseStatus();
        LoadBusinessSettings();
        LoadAutomaticBackupSettings();
        LoadSupportDiagnosticsStatus();
    }

    private void LoadBusinessSettings()
    {
        try
        {
            AppSetting setting =
                _appSettingService.GetSettings();

            InterestRateSettingTextBox.Text =
                setting.InterestRatePercent.ToString(
                    "0.##",
                    CultureInfo.CurrentCulture);

            InterestPeriodDaysSettingTextBox.Text =
                setting.InterestPeriodDays.ToString(
                    CultureInfo.CurrentCulture);

            BusinessSettingSavedStatusText.Text =
                $"บันทึกล่าสุด: {setting.UpdatedAt:dd/MM/yyyy HH:mm}";

            UpdateInterestPreview();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถอ่านการตั้งค่าได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SaveBusinessSettings_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (!TryParseSettingDecimal(
                InterestRateSettingTextBox.Text,
                out decimal interestRate))
        {
            MessageBox.Show(
                "กรุณาระบุอัตราดอกเบี้ยเป็นตัวเลข เช่น 5 หรือ 5.5",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            InterestRateSettingTextBox.Focus();
            return;
        }

        if (!int.TryParse(
                InterestPeriodDaysSettingTextBox.Text.Trim(),
                out int periodDays))
        {
            MessageBox.Show(
                "กรุณาระบุจำนวนวันต่อรอบเป็นตัวเลขจำนวนเต็ม เช่น 15",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            InterestPeriodDaysSettingTextBox.Focus();
            return;
        }

        try
        {
            AppSetting saved =
                _appSettingService.SaveSettings(
                    interestRate,
                    periodDays);

            BusinessSettingSavedStatusText.Text =
                $"บันทึกล่าสุด: {saved.UpdatedAt:dd/MM/yyyy HH:mm}";

            UpdateInterestPreview();

            TryAutomaticBackupAfterDataChange();

            MessageBox.Show(
                "บันทึกการตั้งค่าเรียบร้อย\n\n" +
                $"ดอกเบี้ย: {saved.InterestRatePercent:0.##}% ต่อรอบ\n" +
                $"ระยะเวลา: {saved.InterestPeriodDays:N0} วันต่อรอบ",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถบันทึกการตั้งค่าได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void BusinessSettingInput_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateInterestPreview();
    }

    private void UpdateInterestPreview()
    {
        const decimal examplePrincipal = 10000m;

        if (!TryParseSettingDecimal(
                InterestRateSettingTextBox.Text,
                out decimal interestRate) ||
            !int.TryParse(
                InterestPeriodDaysSettingTextBox.Text.Trim(),
                out int periodDays) ||
            interestRate <= 0m ||
            periodDays <= 0)
        {
            InterestPreviewText.Text =
                "ดอกเบี้ยต่อรอบ: กรุณาระบุค่าที่ถูกต้อง";
            return;
        }

        decimal interest =
            _appSettingService.CalculateInterestForOnePeriod(
                examplePrincipal,
                interestRate);

        InterestPreviewText.Text =
            $"ดอกเบี้ยต่อรอบ: {interest:N2} บาท • ทุก {periodDays:N0} วัน";
    }

    private static bool TryParseSettingDecimal(
        string text,
        out decimal value)
    {
        string cleaned = text.Trim();

        return decimal.TryParse(
                   cleaned,
                   NumberStyles.Number,
                   CultureInfo.CurrentCulture,
                   out value) ||
               decimal.TryParse(
                   cleaned,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out value);
    }

    private void LoadMachineIdentity()
    {
        try
        {
            MachineIdentity identity =
                _machineIdentityService.GetIdentity();

            MachineIdText.Text =
                identity.MachineId;

            MachineFingerprintVersionText.Text =
                $"Fingerprint: {identity.FingerprintVersion}";
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MachineIdText.Text =
                "ไม่สามารถสร้าง Machine ID ได้";

            MachineFingerprintVersionText.Text =
                ex.Message;
        }
    }

    private void LoadLicenseStatus()
    {
        LicenseValidationResult result =
            _licenseValidationService
                .ValidateInstalledLicense();

        if (result.IsValid &&
            result.Payload is not null)
        {
            LicenseFoundationStatusText.Text =
                $"{result.LicenseTypeText} • {result.Payload.CustomerName}";

            LicenseFoundationStatusText.Foreground =
                Brushes.ForestGreen;

            string clockProtectionText =
                result.Payload.LicenseType == "Trial"
                    ? " • ป้องกันย้อนเวลา: เปิดใช้งาน"
                    : string.Empty;

            LicenseDetailStatusText.Text =
                $"Machine ID: {result.Payload.MachineId} • " +
                $"หมดอายุ: {result.ExpiryText} • " +
                $"Key: {result.Payload.KeyId}" +
                clockProtectionText;
        }
        else
        {
            LicenseFoundationStatusText.Text =
                result.Message;

            LicenseFoundationStatusText.Foreground =
                Brushes.Firebrick;

            LicenseDetailStatusText.Text =
                $"ตำแหน่ง License: {LicensePaths.LicenseFile}";
        }
    }

    private void ImportLicenseFromSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog =
            new()
            {
                Title = "เลือก ManaChai License",
                Filter =
                    "ManaChai License (*.license)|*.license|All files (*.*)|*.*",
                DefaultExt = ".license",
                CheckFileExists = true,
                Multiselect = false
            };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        LicenseValidationResult result =
            _licenseValidationService
                .InstallLicense(
                    dialog.FileName);

        LoadLicenseStatus();

        if (!result.IsValid)
        {
            MessageBox.Show(
                $"ไม่สามารถติดตั้ง License นี้ได้\n\n{result.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show(
            "ติดตั้ง License เรียบร้อย\n\n" +
            $"ประเภท: {result.LicenseTypeText}\n" +
            $"หมดอายุ: {result.ExpiryText}",
            AppInfo.StoreName,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CopyMachineIdButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            MachineIdentity identity =
                _machineIdentityService.GetIdentity();

            Clipboard.SetText(
                identity.MachineId);

            MessageBox.Show(
                $"คัดลอกรหัสเครื่องแล้ว\n\n{identity.MachineId}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถคัดลอกรหัสเครื่องได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoadAutomaticBackupSettings()
    {
        AutomaticBackupSettings settings =
            _automaticBackupService.GetSettings();

        AutomaticBackupEnabledCheckBox.IsChecked =
            settings.IsEnabled;

        AutomaticBackupFolderTextBox.Text =
            settings.BackupFolder;

        UpdateAutomaticBackupStatus(
            settings);
    }

    private void ChooseAutomaticBackupFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFolderDialog dialog =
            new()
            {
                Title = "เลือกโฟลเดอร์สำหรับสำรองข้อมูลอัตโนมัติ",
                Multiselect = false
            };

        string currentFolder =
            AutomaticBackupFolderTextBox.Text.Trim();

        if (!string.IsNullOrWhiteSpace(
                currentFolder) &&
            Directory.Exists(
                currentFolder))
        {
            dialog.InitialDirectory =
                currentFolder;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        AutomaticBackupFolderTextBox.Text =
            dialog.FolderName;
    }

    private void SaveAutomaticBackupSettingsButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        bool isEnabled =
            AutomaticBackupEnabledCheckBox.IsChecked == true;

        try
        {
            AutomaticBackupSettings settings =
                _automaticBackupService.SaveConfiguration(
                    isEnabled,
                    AutomaticBackupFolderTextBox.Text);

            if (!isEnabled)
            {
                LoadAutomaticBackupSettings();

                MessageBox.Show(
                    "ปิดการสำรองข้อมูลอัตโนมัติแล้ว\n\n" +
                    "ปุ่มสำรองข้อมูลด้วยตนเองยังใช้งานได้ตามปกติ",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            AutomaticBackupExecutionResult result =
                _automaticBackupService.RunAutomaticBackup();

            LoadAutomaticBackupSettings();

            if (result.IsSuccess)
            {
                string locationNote =
                    _automaticBackupService
                        .IsRecommendedExternalLocation(
                            settings.BackupFolder)
                        ? string.Empty
                        : "\n\nหมายเหตุ: ตำแหน่งนี้อยู่ Drive เดียวกับฐานข้อมูล แนะนำให้ใช้ Flash Drive, External Drive หรือ Drive อื่นเพื่อป้องกันกรณี Disk เสีย";

                MessageBox.Show(
                    "เปิด Auto Backup และทดสอบสำรองข้อมูลเรียบร้อย\n\n" +
                    $"ไฟล์:\n{result.FilePath}" +
                    locationNote,
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(
                    "บันทึกการตั้งค่าแล้ว แต่ทดสอบ Auto Backup ไม่สำเร็จ\n\n" +
                    $"{result.ErrorMessage}\n\n" +
                    "กรุณาตรวจว่า Drive หรือโฟลเดอร์ปลายทางยังใช้งานได้",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถบันทึกการตั้งค่า Auto Backup ได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RunAutomaticBackupAfterStartup()
    {
        AutomaticBackupExecutionResult result =
            _automaticBackupService.RunAutomaticBackup();

        if (!result.IsFailed)
        {
            return;
        }

        AppLog.Warning(
            $"Automatic backup failed after startup: {result.ErrorMessage}");

        MessageBox.Show(
            "โปรแกรมเปิดใช้งานได้ตามปกติ แต่ Auto Backup ไม่สำเร็จ\n\n" +
            $"{result.ErrorMessage}\n\n" +
            "กรุณาตรวจ Drive สำรองข้อมูลที่หน้า ตั้งค่า",
            "ตรวจสอบ Auto Backup",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void TryAutomaticBackupAfterDataChange()
    {
        AutomaticBackupExecutionResult result =
            _automaticBackupService.RunAutomaticBackup();

        if (SettingsContent.Visibility ==
            Visibility.Visible)
        {
            LoadAutomaticBackupSettings();
        }

        if (!result.IsFailed)
        {
            return;
        }

        AppLog.Warning(
            $"Automatic backup failed after data change: {result.ErrorMessage}");

        MessageBox.Show(
            "ข้อมูลถูกบันทึกเรียบร้อยแล้ว แต่ Auto Backup ไม่สำเร็จ\n\n" +
            $"{result.ErrorMessage}\n\n" +
            "ข้อมูลในโปรแกรมไม่สูญหาย แต่ควรตรวจ Drive สำรองข้อมูลโดยเร็ว",
            "Auto Backup ไม่สำเร็จ",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void UpdateAutomaticBackupStatus(
        AutomaticBackupSettings settings)
    {
        if (!settings.IsEnabled)
        {
            AutomaticBackupStatusText.Text =
                "สถานะ: ปิดใช้งาน";

            AutomaticBackupStatusText.Foreground =
                Brushes.DimGray;

            AutomaticBackupLocationHintText.Text =
                "แนะนำ: Flash Drive / External Drive / Drive อื่นจากฐานข้อมูล";

            AutomaticBackupLocationHintText.Foreground =
                Brushes.DimGray;

            return;
        }

        if (!string.IsNullOrWhiteSpace(
                settings.LastError) &&
            (!settings.LastSuccessfulBackupAt.HasValue ||
             (settings.LastAttemptAt.HasValue &&
              settings.LastAttemptAt.Value >
              settings.LastSuccessfulBackupAt.Value)))
        {
            AutomaticBackupStatusText.Text =
                $"สถานะ: ต้องตรวจสอบ • ล่าสุดไม่สำเร็จ • {settings.LastAttemptAt:dd/MM/yyyy HH:mm}\n" +
                settings.LastError;

            AutomaticBackupStatusText.Foreground =
                Brushes.Firebrick;
        }
        else if (settings.LastSuccessfulBackupAt.HasValue)
        {
            AutomaticBackupStatusText.Text =
                $"สถานะ: พร้อมใช้งาน • สำรองล่าสุด {settings.LastSuccessfulBackupAt:dd/MM/yyyy HH:mm}";

            AutomaticBackupStatusText.Foreground =
                Brushes.ForestGreen;
        }
        else
        {
            AutomaticBackupStatusText.Text =
                "สถานะ: เปิดใช้งาน • ยังไม่มี Backup สำเร็จ";

            AutomaticBackupStatusText.Foreground =
                Brushes.DarkOrange;
        }

        bool recommended =
            _automaticBackupService
                .IsRecommendedExternalLocation(
                    settings.BackupFolder);

        AutomaticBackupLocationHintText.Text =
            recommended
                ? $"เก็บย้อนหลัง {AutomaticBackupService.RetentionDays} วัน • ตำแหน่ง Backup อยู่คนละ Drive กับฐานข้อมูล"
                : $"เก็บย้อนหลัง {AutomaticBackupService.RetentionDays} วัน • แนะนำให้เลือก Flash Drive, External Drive หรือ Drive อื่นจากฐานข้อมูล";

        AutomaticBackupLocationHintText.Foreground =
            recommended
                ? Brushes.ForestGreen
                : Brushes.DarkOrange;
    }

    private void LoadSupportDiagnosticsStatus()
    {
        SupportDiagnosticsLogPathText.Text =
            $"Technical Log: {_supportDiagnosticsService.LogFolder}";

        SupportDiagnosticsStatusText.Text =
            $"เก็บ Log ย้อนหลัง {AppLog.RetentionDays} วัน • ชุด Support จะรวม Log ล่าสุดไม่เกิน {SupportDiagnosticsService.IncludedLogDays} วัน";

        SupportDiagnosticsStatusText.Foreground =
            Brushes.DimGray;
    }

    private void OpenTechnicalLogFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(
                _supportDiagnosticsService.LogFolder);

            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        _supportDiagnosticsService.LogFolder,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not open technical log folder.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดโฟลเดอร์ Log ได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CreateSupportPackageButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            SupportDiagnosticsStatusText.Text =
                "กำลังสร้างชุดข้อมูล Support...";

            SupportDiagnosticsStatusText.Foreground =
                Brushes.DarkOrange;

            SupportPackageResult result =
                _supportDiagnosticsService
                    .CreateSupportPackage();

            SupportDiagnosticsStatusText.Text =
                $"สร้างล่าสุด {result.CreatedAt:dd/MM/yyyy HH:mm} • {result.LogFileCount:N0} Log";

            SupportDiagnosticsStatusText.Foreground =
                Brushes.ForestGreen;

            MessageBoxResult openResult =
                MessageBox.Show(
                    "สร้างชุดข้อมูล Support เรียบร้อย\n\n" +
                    $"ไฟล์:\n{result.FilePath}\n\n" +
                    "ภายในไม่มี Database, Backup, ไฟล์ License, Private Key หรือข้อมูลลูกค้า\n\n" +
                    "ต้องการเปิดโฟลเดอร์ที่เก็บไฟล์หรือไม่?",
                    "สร้างชุดข้อมูล Support สำเร็จ",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

            if (openResult ==
                MessageBoxResult.Yes)
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments =
                            $"/select,\"{result.FilePath}\"",
                        UseShellExecute = true
                    });
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not create support diagnostics package.",
                ex);

            SupportDiagnosticsStatusText.Text =
                "สร้างชุดข้อมูล Support ไม่สำเร็จ";

            SupportDiagnosticsStatusText.Foreground =
                Brushes.Firebrick;

            MessageBox.Show(
                $"ไม่สามารถสร้างชุดข้อมูล Support ได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void BackupDatabaseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SaveFileDialog dialog =
            new()
            {
                Title = $"สำรองข้อมูล {ManaChaiLeasing.AppInfo.StoreName}",
                Filter =
                    "ManaChaiLeasing Backup (*.db)|*.db|All files (*.*)|*.*",
                DefaultExt = ".db",
                AddExtension = true,
                OverwritePrompt = true,
                FileName =
                    $"ManaChaiLeasing_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            DatabaseBackupResult result =
                _databaseBackupService.CreateBackup(
                    dialog.FileName);

            DatabaseBackupStatusText.Text =
                $"สำรองล่าสุด: {result.CreatedAt:dd/MM/yyyy HH:mm} • {result.FilePath}";

            DatabaseBackupStatusText.Foreground =
                Brushes.ForestGreen;

            MessageBox.Show(
                "สำรองข้อมูลเรียบร้อย\n\n" +
                $"ไฟล์:\n{result.FilePath}\n\n" +
                $"ขนาด: {FormatFileSize(result.FileSizeBytes)}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            DatabaseBackupStatusText.Text =
                "สำรองข้อมูลไม่สำเร็จ";

            DatabaseBackupStatusText.Foreground =
                Brushes.Firebrick;

            MessageBox.Show(
                $"ไม่สามารถสำรองข้อมูลได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RestoreDatabaseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog =
            new()
            {
                Title = "เลือกไฟล์ Backup ที่ต้องการกู้คืน",
                Filter =
                    "ManaChaiLeasing Backup (*.db)|*.db|All files (*.*)|*.*",
                DefaultExt = ".db",
                CheckFileExists = true,
                Multiselect = false
            };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            DatabaseBackupInfo info =
                _databaseBackupService.InspectBackup(
                    dialog.FileName);

            MessageBoxResult confirm =
                MessageBox.Show(
                    "ตรวจพบไฟล์ Backup ที่ใช้งานได้\n\n" +
                    $"ชื่อร้าน: {info.StoreName}\n" +
                    $"ลูกค้า: {info.CustomerCount:N0} ราย\n" +
                    $"ตั๋วจำนำ: {info.PawnTicketCount:N0} ใบ\n" +
                    $"ประวัติรายการ: {info.TransactionCount:N0} รายการ\n" +
                    $"วันที่ไฟล์: {info.FileModifiedAt:dd/MM/yyyy HH:mm}\n\n" +
                    "ข้อมูลปัจจุบันในโปรแกรมจะถูกแทนด้วยข้อมูลจากไฟล์นี้\n" +
                    "ระบบจะสำรองข้อมูลปัจจุบันให้อัตโนมัติก่อนกู้คืน\n\n" +
                    "ยืนยันกู้คืนข้อมูลหรือไม่?",
                    "ยืนยันกู้คืนข้อมูล",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            DatabaseRestoreResult result =
                _databaseBackupService.RestoreBackup(
                    dialog.FileName);

            DatabaseBackupStatusText.Text =
                "กู้คืนข้อมูลสำเร็จ • กำลังปิดโปรแกรม";

            DatabaseBackupStatusText.Foreground =
                Brushes.ForestGreen;

            MessageBox.Show(
                "กู้คืนข้อมูลเรียบร้อย\n\n" +
                "ระบบได้สร้าง Safety Backup ของข้อมูลก่อนกู้คืนไว้ที่:\n" +
                $"{result.SafetyBackupPath}\n\n" +
                "โปรแกรมจะปิดในตอนนี้ กรุณาเปิดใหม่อีกครั้งเพื่อใช้งานฐานข้อมูลที่กู้คืน",
                "กู้คืนข้อมูลสำเร็จ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            DatabaseBackupStatusText.Text =
                "กู้คืนข้อมูลไม่สำเร็จ";

            DatabaseBackupStatusText.Foreground =
                Brushes.Firebrick;

            MessageBox.Show(
                $"ไม่สามารถกู้คืนข้อมูลได้\n\n{ex.Message}\n\n" +
                "ฐานข้อมูลปัจจุบันจะไม่ถูกเปลี่ยน หากกระบวนการ Restore ยังไม่ผ่านการตรวจสอบ",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static string FormatFileSize(
        long bytes)
    {
        const double oneKb = 1024d;
        const double oneMb = oneKb * 1024d;

        if (bytes >= oneMb)
        {
            return $"{bytes / oneMb:N2} MB";
        }

        if (bytes >= oneKb)
        {
            return $"{bytes / oneKb:N2} KB";
        }

        return $"{bytes:N0} bytes";
    }

    private void SearchExistingCustomer_Click(
        object sender,
        RoutedEventArgs e)
    {
        CustomerLookupWindow lookupWindow = new()
        {
            Owner = this
        };

        bool? result = lookupWindow.ShowDialog();

        if (result == true &&
            lookupWindow.SelectedCustomer is not null)
        {
            _pendingThaiIdCardData = null;
            FillCustomerForm(lookupWindow.SelectedCustomer);
        }
    }

    private void SaveCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_thaiIdMockDataLoaded)
        {
            MessageBox.Show(
                "ข้อมูลในฟอร์มมาจาก DEV MOCK และไม่อนุญาตให้บันทึกลงฐานข้อมูล\n\n" +
                "กรุณากด ล้างฟอร์ม แล้วกรอกข้อมูลจริงก่อนบันทึก",
                "ป้องกันข้อมูลจำลอง",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string firstName = FirstNameTextBox.Text.Trim();
        string lastName = LastNameTextBox.Text.Trim();
        string citizenId = CitizenIdTextBox.Text.Trim();
        string ageText = AgeTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show(
                "กรุณากรอกชื่อและนามสกุลลูกค้า",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        if (!string.IsNullOrWhiteSpace(citizenId) &&
            (citizenId.Length != 13 ||
             !citizenId.All(char.IsDigit)))
        {
            MessageBox.Show(
                "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก หรือเว้นว่างไว้",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            CitizenIdTextBox.Focus();
            return;
        }

        int? age = null;

        if (!string.IsNullOrWhiteSpace(ageText))
        {
            if (!int.TryParse(ageText, out int parsedAge) ||
                parsedAge < 1 ||
                parsedAge > 120)
            {
                MessageBox.Show(
                    "อายุต้องเป็นตัวเลขระหว่าง 1 - 120 ปี หรือเว้นว่างไว้",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AgeTextBox.Focus();
                return;
            }

            age = parsedAge;
        }

        try
        {
            Customer input = new()
            {
                FirstName = firstName,
                LastName = lastName,
                CitizenId = citizenId,
                Age = age,
                Phone = PhoneTextBox.Text,
                Address = AddressTextBox.Text
            };

            Customer savedCustomer =
                _customerService.SaveCustomer(
                    input,
                    _selectedCustomerId);

            _selectedCustomerId = savedCustomer.Id;

            CustomerRecordStatusText.Text =
                $"บันทึกแล้ว • ลูกค้า #{savedCustomer.Id}";

            CustomerRecordStatusText.Foreground =
                Brushes.ForestGreen;

            TryAutomaticBackupAfterDataChange();

            MessageBox.Show(
                $"บันทึกข้อมูลลูกค้าเรียบร้อย\n\n{savedCustomer.FirstName} {savedCustomer.LastName}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถบันทึกข้อมูลลูกค้าได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void FillCustomerForm(Customer customer)
    {
        _thaiIdMockDataLoaded = false;
        _selectedCustomerId = customer.Id;

        FirstNameTextBox.Text = customer.FirstName;
        LastNameTextBox.Text = customer.LastName;
        CitizenIdTextBox.Text = customer.CitizenId ?? string.Empty;
        AgeTextBox.Text = customer.Age?.ToString() ?? string.Empty;
        PhoneTextBox.Text = customer.Phone ?? string.Empty;
        AddressTextBox.Text = customer.Address ?? string.Empty;

        CustomerRecordStatusText.Text =
            $"ลูกค้าเดิม • #{customer.Id}";

        CustomerRecordStatusText.Foreground =
            Brushes.ForestGreen;

        FirstNameTextBox.Focus();
    }

    private void ResetCustomerState()
    {
        _thaiIdMockDataLoaded = false;
        _pendingThaiIdCardData = null;
        _selectedCustomerId = null;

        CustomerRecordStatusText.Text =
            "ลูกค้าใหม่ • ยังไม่ได้บันทึก";

        CustomerRecordStatusText.Foreground =
            Brushes.DimGray;
    }

    private void AssetCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateProductForm();
        UpdateAssetPreview();
    }

    private void FormComboBox_PreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox ||
            !comboBox.IsEnabled)
        {
            return;
        }

        // สำหรับ Editable ComboBox ให้ mouse-down ทำหน้าที่ focus/caret ตามปกติ
        // แล้วเปิดรายการทันทีตอน mouse-up ของ click แรก
        // เพื่อไม่ให้เกิดอาการต้องคลิกครั้งแรกเพื่อ Focus ก่อน
        if (!comboBox.IsDropDownOpen)
        {
            e.Handled = true;
            comboBox.IsDropDownOpen = true;
        }

        if (comboBox.IsEditable &&
            comboBox.Template.FindName(
                "PART_EditableTextBox",
                comboBox) is TextBox editableTextBox)
        {
            editableTextBox.Focus();

            if (editableTextBox.SelectionLength == 0)
            {
                editableTextBox.CaretIndex =
                    editableTextBox.Text?.Length ?? 0;
            }
        }
    }

    private void SmartField_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateAssetPreview();
    }

    private void SmartField_KeyUp(
        object sender,
        KeyEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateAssetPreview();
    }

    private void SmartField_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateAssetPreview();
    }

    private void UpdateProductForm()
    {
        if (MobileProductPanel is null ||
            ItProductPanel is null ||
            ElectricalProductPanel is null ||
            OtherProductPanel is null)
        {
            return;
        }

        MobileProductPanel.Visibility = Visibility.Collapsed;
        ItProductPanel.Visibility = Visibility.Collapsed;
        ElectricalProductPanel.Visibility = Visibility.Collapsed;
        OtherProductPanel.Visibility = Visibility.Collapsed;

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                MobileProductPanel.Visibility = Visibility.Visible;
                break;

            case 1:
                ItProductPanel.Visibility = Visibility.Visible;
                break;

            case 2:
                ElectricalProductPanel.Visibility = Visibility.Visible;
                break;

            default:
                OtherProductPanel.Visibility = Visibility.Visible;
                break;
        }
    }

    private void UpdateAssetPreview()
    {
        if (_isInitializing || AssetPreviewText is null)
        {
            return;
        }

        string productSummary = BuildProductSummary();

        AssetPreviewText.Text =
            string.IsNullOrWhiteSpace(productSummary)
                ? "กรอกข้อมูลสินค้า แล้วระบบจะสร้างรายละเอียดสรุปให้อัตโนมัติ"
                : productSummary;
    }

    private static string GetComboText(ComboBox comboBox)
    {
        if (!string.IsNullOrWhiteSpace(comboBox.Text))
        {
            return comboBox.Text.Trim();
        }

        if (comboBox.SelectedItem is ComboBoxItem selectedItem &&
            selectedItem.Content is not null)
        {
            return selectedItem.Content.ToString()?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private static void AddIfValue(
        List<string> parts,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(value.Trim());
        }
    }

    private static void AddLabeledValue(
        List<string> parts,
        string label,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value.Trim()}");
        }
    }

    private void ReadThaiIdCardButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ThaiIdReaderDetectionResult detection =
            RefreshThaiIdReaderStatus();

        if (detection.Status ==
            ThaiIdReaderStatus.Ready)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                ThaiIdCardReadResult readResult =
                    _thaiIdCardReaderService.ReadCard();

                if (!readResult.Success ||
                    readResult.Data is null)
                {
                    MessageBox.Show(
                        $"{readResult.UserMessage}\n\n" +
                        "หาก Reader ใช้งานไม่ได้ สามารถกรอกข้อมูลลูกค้าด้วยตนเองได้ตามปกติ",
                        "อ่านบัตรประชาชนไม่สำเร็จ",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                ShowThaiIdCardPreview(
                    readResult.Data);
            }
            finally
            {
                Mouse.OverrideCursor = null;
                RefreshThaiIdReaderStatus();
            }

            return;
        }

        switch (detection.Status)
        {
            case ThaiIdReaderStatus.ReaderFoundNoCard:
                MessageBox.Show(
                    "พบเครื่องอ่านบัตรแล้ว แต่ยังไม่พบบัตรประชาชน\n\n" +
                    "กรุณาเสียบบัตรให้สุด แล้วกด อ่านบัตรประชาชน อีกครั้ง\n\n" +
                    "หากไม่ต้องการใช้ Reader สามารถกรอกข้อมูลด้วยตนเองได้ตามปกติ",
                    "ยังไม่พบบัตรประชาชน",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break;

            case ThaiIdReaderStatus.NoReader:
                MessageBox.Show(
                    "ไม่พบเครื่องอ่านบัตรประชาชนที่ Windows รู้จัก\n\n" +
                    "ตรวจสาย USB / Driver / Smart Card service แล้วลองใหม่\n\n" +
                    "การรับจำนำยังใช้งานต่อได้โดยกรอกข้อมูลด้วยตนเอง",
                    "ไม่พบ Thai ID Reader",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                break;

            case ThaiIdReaderStatus.PcScUnavailable:
            case ThaiIdReaderStatus.Error:
                MessageBox.Show(
                    $"{detection.StatusText}\n\n" +
                    "การรับจำนำยังใช้งานต่อได้โดยกรอกข้อมูลด้วยตนเอง",
                    "Thai ID Reader",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                break;
        }
    }

    private void ThaiIdMockButton_Click(
        object sender,
        RoutedEventArgs e)
    {
#if DEBUG
        try
        {
            string citizenIdOverride =
                CitizenIdTextBox.Text.Trim();

            ThaiIdCardData mockData =
                _thaiIdDevelopmentMockService.CreateParsedMockData(
                    citizenIdOverride);

            ShowThaiIdCardPreview(
                mockData);
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Thai ID development mock parser failed.",
                ex);

            MessageBox.Show(
                $"Mock Parser Test ไม่สำเร็จ\n\n{ex.Message}",
                "Thai ID DEV Test",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
#endif
    }

    private void ShowThaiIdCardPreview(
        ThaiIdCardData cardData)
    {
        ThaiIdCardPreviewWindow previewWindow =
            new(cardData)
            {
                Owner = this
            };

        bool? result =
            previewWindow.ShowDialog();

        if (result == true)
        {
            ApplyThaiIdCardDataToCustomerForm(
                cardData);
        }
    }

    private void ApplyThaiIdCardDataToCustomerForm(
        ThaiIdCardData cardData)
    {
        ResetCustomerState();

        _pendingThaiIdCardData =
            cardData;

        _thaiIdMockDataLoaded =
            cardData.Source ==
            ThaiIdCardDataSource.DevelopmentMock;

        Customer? matchedCustomer =
            _customerService.FindByCitizenId(
                cardData.CitizenId);

        if (matchedCustomer is not null)
        {
            ApplyExistingCustomerMatchedFromThaiId(
                matchedCustomer,
                cardData);

            return;
        }

        ApplyNewCustomerDataFromThaiId(
            cardData);
    }

    private void ApplyExistingCustomerMatchedFromThaiId(
        Customer customer,
        ThaiIdCardData cardData)
    {
        _selectedCustomerId = customer.Id;

        // Always start from the CURRENT values stored in the database.
        // Card values are applied only after an explicit 2N.4 review choice.
        FirstNameTextBox.Text = customer.FirstName;
        LastNameTextBox.Text = customer.LastName;
        CitizenIdTextBox.Text = customer.CitizenId ?? cardData.CitizenId;
        AgeTextBox.Text = customer.Age?.ToString() ?? string.Empty;
        PhoneTextBox.Text = customer.Phone ?? string.Empty;
        AddressTextBox.Text = customer.Address ?? string.Empty;

        ThaiIdCustomerUpdateReviewWindow reviewWindow =
            new(
                customer,
                cardData)
            {
                Owner = this
            };

        bool? reviewResult =
            reviewWindow.ShowDialog();

        int selectedCount = 0;

        if (reviewResult == true &&
            reviewWindow.Selection is not null)
        {
            ThaiIdCustomerUpdateSelection selection =
                reviewWindow.Selection;

            if (selection.FirstName)
            {
                FirstNameTextBox.Text =
                    cardData.ThaiFirstName.Trim();
            }

            if (selection.LastName)
            {
                LastNameTextBox.Text =
                    cardData.ThaiLastName.Trim();
            }

            if (selection.Age)
            {
                int? cardAge =
                    cardData.CalculateAge(
                        DateTime.Today);

                AgeTextBox.Text =
                    cardAge?.ToString() ?? string.Empty;
            }

            if (selection.Address)
            {
                AddressTextBox.Text =
                    cardData.Address.Trim();
            }

            selectedCount =
                selection.SelectedCount;
        }

        // Citizen ID is the identity key that produced the match.
        // Never change it here. Phone is not present on the ID card,
        // so it is also always preserved from the database.
        CitizenIdTextBox.Text =
            customer.CitizenId ?? cardData.CitizenId;

        PhoneTextBox.Text =
            customer.Phone ?? string.Empty;

        if (_thaiIdMockDataLoaded)
        {
            CustomerRecordStatusText.Text =
                selectedCount > 0
                    ? $"DEV MOCK • ลูกค้าเดิม #{customer.Id} • เลือกอัปเดต {selectedCount} ช่อง • ห้ามบันทึก"
                    : $"DEV MOCK • ลูกค้าเดิม #{customer.Id} • ใช้ข้อมูลเดิม • ห้ามบันทึก";

            CustomerRecordStatusText.Foreground =
                Brushes.Firebrick;
        }
        else if (selectedCount > 0)
        {
            CustomerRecordStatusText.Text =
                $"ลูกค้าเดิมจากบัตร • #{customer.Id} • ปรับข้อมูลในฟอร์ม {selectedCount} ช่อง • ยังไม่ได้บันทึก";

            CustomerRecordStatusText.Foreground =
                Brushes.DarkOrange;
        }
        else
        {
            CustomerRecordStatusText.Text =
                $"ลูกค้าเดิมจากบัตร • #{customer.Id} • ใช้ข้อมูลเดิม";

            CustomerRecordStatusText.Foreground =
                Brushes.ForestGreen;
        }

        FirstNameTextBox.Focus();
    }

    private void ApplyNewCustomerDataFromThaiId(
        ThaiIdCardData cardData)
    {
        FirstNameTextBox.Text =
            cardData.ThaiFirstName;

        LastNameTextBox.Text =
            cardData.ThaiLastName;

        CitizenIdTextBox.Text =
            cardData.CitizenId;

        int? age =
            cardData.CalculateAge(DateTime.Today);

        AgeTextBox.Text =
            age?.ToString() ?? string.Empty;

        // ID card does not contain the shop customer's phone number.
        // Never overwrite a phone value from card-reading workflow.
        AddressTextBox.Text =
            cardData.Address;

        if (_thaiIdMockDataLoaded)
        {
            CustomerRecordStatusText.Text =
                "DEV MOCK • ไม่พบลูกค้าเดิม • ห้ามบันทึก";

            CustomerRecordStatusText.Foreground =
                Brushes.Firebrick;
        }
        else
        {
            CustomerRecordStatusText.Text =
                "ลูกค้าใหม่จากบัตร • ยังไม่ได้บันทึก";

            CustomerRecordStatusText.Foreground =
                Brushes.DarkBlue;
        }

        FirstNameTextBox.Focus();
    }

    private void ConfigureThaiIdDevelopmentTools()
    {
#if DEBUG
        ThaiIdMockButton.Visibility = Visibility.Visible;
#else
        ThaiIdMockButton.Visibility = Visibility.Collapsed;
#endif
    }

    private ThaiIdReaderDetectionResult RefreshThaiIdReaderStatus()
    {
        ThaiIdReaderDetectionResult result =
            _thaiIdCardReaderService.Detect();

        ThaiIdReaderStatusText.Text =
            result.StatusText;

        ThaiIdReaderNameText.Text =
            string.IsNullOrWhiteSpace(
                result.ReaderName)
                ? string.Empty
                : $"Reader: {result.ReaderName}";

        switch (result.Status)
        {
            case ThaiIdReaderStatus.Ready:
                ThaiIdReaderStatusText.Foreground = Brushes.ForestGreen;
                ThaiIdReaderDot.Fill = Brushes.ForestGreen;
                break;

            case ThaiIdReaderStatus.ReaderFoundNoCard:
                ThaiIdReaderStatusText.Foreground = Brushes.DarkOrange;
                ThaiIdReaderDot.Fill = Brushes.DarkOrange;
                break;

            case ThaiIdReaderStatus.NoReader:
            case ThaiIdReaderStatus.PcScUnavailable:
                ThaiIdReaderStatusText.Foreground = Brushes.DimGray;
                ThaiIdReaderDot.Fill = Brushes.Gray;
                break;

            default:
                ThaiIdReaderStatusText.Foreground = Brushes.Firebrick;
                ThaiIdReaderDot.Fill = Brushes.Firebrick;
                break;
        }

        if (!string.IsNullOrWhiteSpace(
                result.TechnicalMessage) &&
            result.Status == ThaiIdReaderStatus.Error)
        {
            AppLog.Warning(
                $"Thai ID reader status: {result.TechnicalMessage}");
        }

        return result;
    }

    private void SavePawnTicket_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isSavingPawnTicket)
        {
            AppLog.Warning(
                "Duplicate pawn-ticket save action blocked at UI.");

            return;
        }

        _isSavingPawnTicket = true;
        SavePawnTicketButton.IsEnabled = false;
        SavePawnTicketButton.Content = "กำลังบันทึก...";

        try
        {
            if (!TryBuildCustomerInput(out Customer customer))
            {
                return;
            }

            string ticketNumber = TicketNumberTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ticketNumber))
            {
                MessageBox.Show(
                    "กรุณากรอกหมายเลขตั๋ว",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                TicketNumberTextBox.Focus();
                return;
            }

            if (!PawnDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "กรุณาเลือกวันที่รับจำนำ",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                PawnDatePicker.Focus();
                return;
            }

            if (!TryParsePawnAmount(
                    PawnAmountTextBox.Text,
                    out decimal principalAmount) ||
                principalAmount <= 0)
            {
                MessageBox.Show(
                    "กรุณากรอกยอดเงินจำนำให้ถูกต้อง และต้องมากกว่า 0 บาท",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                PawnAmountTextBox.Focus();
                return;
            }

            string productSummary = BuildProductSummary();

            if (string.IsNullOrWhiteSpace(productSummary))
            {
                MessageBox.Show(
                    "กรุณากรอกรายละเอียดสินค้าอย่างน้อย 1 ช่อง",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            PawnTicket ticket = BuildPawnTicket(
                ticketNumber,
                PawnDatePicker.SelectedDate.Value.Date,
                principalAmount,
                productSummary);

            PawnTicket savedTicket =
                _pawnTicketService.SavePawnTicket(
                    new PawnTicketSaveRequest
                    {
                        SelectedCustomerId = _selectedCustomerId,
                        Customer = customer,
                        Ticket = ticket,
                        SmartLookupValues =
                            BuildSmartLookupEntries()
                    });

            _selectedCustomerId = savedTicket.CustomerId;

            LoadSmartLookupValues();
            TryAutomaticBackupAfterDataChange();

            PawnSaveSuccessWindow successWindow = new(
                savedTicket.TicketNumber,
                savedTicket.PrincipalAmount)
            {
                Owner = this
            };

            successWindow.ShowDialog();

            ClearNewPawnForm();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถบันทึกตั๋วจำนำได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isSavingPawnTicket = false;
            SavePawnTicketButton.IsEnabled = true;
            SavePawnTicketButton.Content = "บันทึกข้อมูล";
        }
    }

    private bool TryBuildCustomerInput(out Customer customer)
    {
        customer = new Customer();

        if (_thaiIdMockDataLoaded)
        {
            MessageBox.Show(
                "ข้อมูลในฟอร์มมาจาก DEV MOCK และไม่อนุญาตให้สร้างตั๋วจำนำ\n\n" +
                "กรุณากด ล้างฟอร์ม แล้วกรอกข้อมูลจริงก่อนบันทึก",
                "ป้องกันข้อมูลจำลอง",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        string firstName = FirstNameTextBox.Text.Trim();
        string lastName = LastNameTextBox.Text.Trim();
        string citizenId = CitizenIdTextBox.Text.Trim();
        string ageText = AgeTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            MessageBox.Show(
                "กรุณากรอกชื่อและนามสกุลลูกค้า",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return false;
        }

        if (!string.IsNullOrWhiteSpace(citizenId) &&
            (citizenId.Length != 13 ||
             !citizenId.All(char.IsDigit)))
        {
            MessageBox.Show(
                "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก หรือเว้นว่างไว้",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            CitizenIdTextBox.Focus();
            return false;
        }

        int? age = null;

        if (!string.IsNullOrWhiteSpace(ageText))
        {
            if (!int.TryParse(ageText, out int parsedAge) ||
                parsedAge < 1 ||
                parsedAge > 120)
            {
                MessageBox.Show(
                    "อายุต้องเป็นตัวเลขระหว่าง 1 - 120 ปี หรือเว้นว่างไว้",
                    ManaChaiLeasing.AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                AgeTextBox.Focus();
                return false;
            }

            age = parsedAge;
        }

        customer = new Customer
        {
            FirstName = firstName,
            LastName = lastName,
            CitizenId = citizenId,
            Age = age,
            Phone = PhoneTextBox.Text,
            Address = AddressTextBox.Text
        };

        return true;
    }

    private PawnTicket BuildPawnTicket(
        string ticketNumber,
        DateTime pawnDate,
        decimal principalAmount,
        string productSummary)
    {
        PawnTicket ticket = new()
        {
            TicketNumber = ticketNumber,
            PawnDate = pawnDate,
            PrincipalAmount = principalAmount,
            AssetCategory = GetAssetCategoryName(),
            ProductSummary = productSummary,
            Note = CleanOptional(PawnNoteTextBox.Text)
        };

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                ticket.Brand = CleanOptional(
                    GetComboText(MobileBrandComboBox));
                ticket.Model = CleanOptional(
                    GetComboText(MobileModelComboBox));
                ticket.CapacityOrSize = CleanOptional(
                    GetComboText(MobileCapacityComboBox));
                ticket.Color = CleanOptional(
                    GetComboText(MobileColorComboBox));
                ticket.ImeiOrSerial = CleanOptional(
                    MobileImeiTextBox.Text);
                ticket.Accessories = CleanOptional(
                    MobileAccessoriesTextBox.Text);
                ticket.Condition = CleanOptional(
                    MobileConditionTextBox.Text);
                break;

            case 1:
                ticket.ProductType = CleanOptional(
                    GetComboText(ItTypeComboBox));
                ticket.Brand = CleanOptional(
                    GetComboText(ItBrandComboBox));
                ticket.Model = CleanOptional(
                    GetComboText(ItModelComboBox));
                ticket.Specification = CleanOptional(
                    ItSpecificationTextBox.Text);
                ticket.ImeiOrSerial = CleanOptional(
                    ItSerialTextBox.Text);
                ticket.Accessories = CleanOptional(
                    ItAccessoriesTextBox.Text);
                ticket.Condition = CleanOptional(
                    ItConditionTextBox.Text);
                break;

            case 2:
                ticket.ProductType = CleanOptional(
                    GetComboText(ElectricalTypeComboBox));
                ticket.Brand = CleanOptional(
                    GetComboText(ElectricalBrandComboBox));
                ticket.Model = CleanOptional(
                    GetComboText(ElectricalModelComboBox));
                ticket.CapacityOrSize = CleanOptional(
                    ElectricalSizeTextBox.Text);
                ticket.ImeiOrSerial = CleanOptional(
                    ElectricalSerialTextBox.Text);
                ticket.Accessories = CleanOptional(
                    ElectricalAccessoriesTextBox.Text);
                ticket.Condition = CleanOptional(
                    ElectricalConditionTextBox.Text);
                break;

            default:
                ticket.ProductType = CleanOptional(
                    OtherTypeTextBox.Text);
                ticket.Brand = CleanOptional(
                    OtherBrandTextBox.Text);
                ticket.Model = CleanOptional(
                    OtherModelTextBox.Text);
                ticket.OtherDetails = CleanOptional(
                    OtherDetailsTextBox.Text);
                ticket.ImeiOrSerial = CleanOptional(
                    OtherSerialTextBox.Text);
                ticket.Accessories = CleanOptional(
                    OtherAccessoriesTextBox.Text);
                ticket.Condition = CleanOptional(
                    OtherConditionTextBox.Text);
                break;
        }

        return ticket;
    }

    private List<SmartLookupEntry> BuildSmartLookupEntries()
    {
        List<SmartLookupEntry> entries = new();

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                AddSmartLookupEntry(
                    entries,
                    "MobileTablet",
                    "Brand",
                    GetComboText(MobileBrandComboBox));
                AddSmartLookupEntry(
                    entries,
                    "MobileTablet",
                    "Model",
                    GetComboText(MobileModelComboBox));
                AddSmartLookupEntry(
                    entries,
                    "MobileTablet",
                    "Capacity",
                    GetComboText(MobileCapacityComboBox));
                AddSmartLookupEntry(
                    entries,
                    "MobileTablet",
                    "Color",
                    GetComboText(MobileColorComboBox));
                break;

            case 1:
                AddSmartLookupEntry(
                    entries,
                    "IT",
                    "ProductType",
                    GetComboText(ItTypeComboBox));
                AddSmartLookupEntry(
                    entries,
                    "IT",
                    "Brand",
                    GetComboText(ItBrandComboBox));
                AddSmartLookupEntry(
                    entries,
                    "IT",
                    "Model",
                    GetComboText(ItModelComboBox));
                break;

            case 2:
                AddSmartLookupEntry(
                    entries,
                    "Electrical",
                    "ProductType",
                    GetComboText(ElectricalTypeComboBox));
                AddSmartLookupEntry(
                    entries,
                    "Electrical",
                    "Brand",
                    GetComboText(ElectricalBrandComboBox));
                AddSmartLookupEntry(
                    entries,
                    "Electrical",
                    "Model",
                    GetComboText(ElectricalModelComboBox));
                break;
        }

        return entries;
    }

    private void LoadSmartLookupValues()
    {
        try
        {
            AddLearnedValues(
                MobileBrandComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "MobileTablet",
                    "Brand"));
            AddLearnedValues(
                MobileModelComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "MobileTablet",
                    "Model"));
            AddLearnedValues(
                MobileCapacityComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "MobileTablet",
                    "Capacity"));
            AddLearnedValues(
                MobileColorComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "MobileTablet",
                    "Color"));

            AddLearnedValues(
                ItTypeComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "IT",
                    "ProductType"));
            AddLearnedValues(
                ItBrandComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "IT",
                    "Brand"));
            AddLearnedValues(
                ItModelComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "IT",
                    "Model"));

            AddLearnedValues(
                ElectricalTypeComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "Electrical",
                    "ProductType"));
            AddLearnedValues(
                ElectricalBrandComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "Electrical",
                    "Brand"));
            AddLearnedValues(
                ElectricalModelComboBox,
                _pawnTicketService.GetSmartLookupValues(
                    "Electrical",
                    "Model"));
        }
        catch
        {
            // Database initialization already reports connection errors.
            // The pawn form can still open without learned dropdown values.
        }
    }

    private static void AddLearnedValues(
        ComboBox comboBox,
        IEnumerable<string> learnedValues)
    {
        HashSet<string> existingValues = comboBox.Items
            .Cast<object>()
            .Select(GetComboItemText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeLookupText)
            .ToHashSet();

        foreach (string value in learnedValues)
        {
            string normalized = NormalizeLookupText(value);

            if (string.IsNullOrWhiteSpace(normalized) ||
                existingValues.Contains(normalized))
            {
                continue;
            }

            comboBox.Items.Add(value);
            existingValues.Add(normalized);
        }
    }

    private static string GetComboItemText(object item)
    {
        if (item is ComboBoxItem comboBoxItem)
        {
            return comboBoxItem.Content?.ToString()?.Trim()
                ?? string.Empty;
        }

        return item?.ToString()?.Trim()
            ?? string.Empty;
    }

    private static string NormalizeLookupText(string value)
    {
        return string.Join(
                " ",
                value.Trim()
                    .Split(
                        ' ',
                        StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static void AddSmartLookupEntry(
        ICollection<SmartLookupEntry> entries,
        string category,
        string fieldType,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        entries.Add(
            new SmartLookupEntry(
                category,
                fieldType,
                value.Trim()));
    }

    private string BuildProductSummary()
    {
        List<string> parts = new();

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                AddIfValue(parts, GetComboText(MobileBrandComboBox));
                AddIfValue(parts, GetComboText(MobileModelComboBox));
                AddIfValue(parts, GetComboText(MobileCapacityComboBox));

                string mobileColor =
                    GetComboText(MobileColorComboBox);

                if (!string.IsNullOrWhiteSpace(mobileColor))
                {
                    parts.Add($"สี {mobileColor}");
                }

                AddLabeledValue(
                    parts,
                    "IMEI",
                    MobileImeiTextBox.Text);
                AddLabeledValue(
                    parts,
                    "อุปกรณ์",
                    MobileAccessoriesTextBox.Text);
                AddLabeledValue(
                    parts,
                    "สภาพ/ตำหนิ",
                    MobileConditionTextBox.Text);
                break;

            case 1:
                AddIfValue(parts, GetComboText(ItTypeComboBox));
                AddIfValue(parts, GetComboText(ItBrandComboBox));
                AddIfValue(parts, GetComboText(ItModelComboBox));
                AddIfValue(parts, ItSpecificationTextBox.Text);
                AddLabeledValue(
                    parts,
                    "Serial",
                    ItSerialTextBox.Text);
                AddLabeledValue(
                    parts,
                    "อุปกรณ์",
                    ItAccessoriesTextBox.Text);
                AddLabeledValue(
                    parts,
                    "สภาพ/ตำหนิ",
                    ItConditionTextBox.Text);
                break;

            case 2:
                AddIfValue(
                    parts,
                    GetComboText(ElectricalTypeComboBox));
                AddIfValue(
                    parts,
                    GetComboText(ElectricalBrandComboBox));
                AddIfValue(
                    parts,
                    GetComboText(ElectricalModelComboBox));
                AddIfValue(parts, ElectricalSizeTextBox.Text);
                AddLabeledValue(
                    parts,
                    "Serial",
                    ElectricalSerialTextBox.Text);
                AddLabeledValue(
                    parts,
                    "อุปกรณ์",
                    ElectricalAccessoriesTextBox.Text);
                AddLabeledValue(
                    parts,
                    "สภาพ/ตำหนิ",
                    ElectricalConditionTextBox.Text);
                break;

            default:
                AddIfValue(parts, OtherTypeTextBox.Text);
                AddIfValue(parts, OtherBrandTextBox.Text);
                AddIfValue(parts, OtherModelTextBox.Text);
                AddIfValue(parts, OtherDetailsTextBox.Text);
                AddLabeledValue(
                    parts,
                    "Serial",
                    OtherSerialTextBox.Text);
                AddLabeledValue(
                    parts,
                    "อุปกรณ์",
                    OtherAccessoriesTextBox.Text);
                AddLabeledValue(
                    parts,
                    "สภาพ/ตำหนิ",
                    OtherConditionTextBox.Text);
                break;
        }

        return string.Join(" / ", parts);
    }

    private string GetAssetCategoryName()
    {
        if (AssetCategoryComboBox.SelectedItem is ComboBoxItem item)
        {
            return item.Content?.ToString()?.Trim()
                ?? "อื่น ๆ";
        }

        return "อื่น ๆ";
    }

    private static bool TryParsePawnAmount(
        string value,
        out decimal amount)
    {
        string normalized = value
            .Trim()
            .Replace(",", string.Empty);

        return decimal.TryParse(
            normalized,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out amount);
    }

    private static string? CleanOptional(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(cleaned)
            ? null
            : cleaned;
    }

    private void ClearNewPawnForm_Click(
        object sender,
        RoutedEventArgs e)
    {
        ClearNewPawnForm();
    }

    private void ClearNewPawnForm()
    {
        _isInitializing = true;

        TicketNumberTextBox.Clear();
        PawnDatePicker.SelectedDate = DateTime.Today;

        FirstNameTextBox.Clear();
        LastNameTextBox.Clear();
        CitizenIdTextBox.Clear();
        AgeTextBox.Clear();
        PhoneTextBox.Clear();
        AddressTextBox.Clear();

        ResetCustomerState();

        AssetCategoryComboBox.SelectedIndex = 0;
        PawnAmountTextBox.Clear();

        MobileBrandComboBox.SelectedIndex = -1;
        MobileBrandComboBox.Text = string.Empty;
        MobileModelComboBox.SelectedIndex = -1;
        MobileModelComboBox.Text = string.Empty;
        MobileCapacityComboBox.SelectedIndex = -1;
        MobileCapacityComboBox.Text = string.Empty;
        MobileColorComboBox.SelectedIndex = -1;
        MobileColorComboBox.Text = string.Empty;
        MobileImeiTextBox.Clear();
        MobileAccessoriesTextBox.Clear();
        MobileConditionTextBox.Clear();

        ItTypeComboBox.SelectedIndex = -1;
        ItTypeComboBox.Text = string.Empty;
        ItBrandComboBox.SelectedIndex = -1;
        ItBrandComboBox.Text = string.Empty;
        ItModelComboBox.SelectedIndex = -1;
        ItModelComboBox.Text = string.Empty;
        ItSpecificationTextBox.Clear();
        ItSerialTextBox.Clear();
        ItAccessoriesTextBox.Clear();
        ItConditionTextBox.Clear();

        ElectricalTypeComboBox.SelectedIndex = -1;
        ElectricalTypeComboBox.Text = string.Empty;
        ElectricalBrandComboBox.SelectedIndex = -1;
        ElectricalBrandComboBox.Text = string.Empty;
        ElectricalModelComboBox.SelectedIndex = -1;
        ElectricalModelComboBox.Text = string.Empty;
        ElectricalSizeTextBox.Clear();
        ElectricalSerialTextBox.Clear();
        ElectricalAccessoriesTextBox.Clear();
        ElectricalConditionTextBox.Clear();

        OtherTypeTextBox.Clear();
        OtherBrandTextBox.Clear();
        OtherModelTextBox.Clear();
        OtherDetailsTextBox.Clear();
        OtherSerialTextBox.Clear();
        OtherAccessoriesTextBox.Clear();
        OtherConditionTextBox.Clear();

        PawnNoteTextBox.Clear();

        _isInitializing = false;

        UpdateProductForm();
        UpdateAssetPreview();

        TicketNumberTextBox.Focus();
    }

    private void PawnTicketSearchFilterComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        LoadPawnTicketSearchResults();
    }

    private PawnTicketSearchFilter GetPawnTicketSearchFilter()
    {
        return PawnTicketSearchFilterComboBox.SelectedIndex switch
        {
            1 => PawnTicketSearchFilter.Active,
            2 => PawnTicketSearchFilter.DueToday,
            3 => PawnTicketSearchFilter.Overdue,
            4 => PawnTicketSearchFilter.Redeemed,
            _ => PawnTicketSearchFilter.All
        };
    }

    private void PawnTicketSearchTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        LoadPawnTicketSearchResults();
    }

    private void LoadPawnTicketSearchResults()
    {
        try
        {
            List<PawnTicketSearchResult> results =
                _pawnTicketSearchService.Search(
                    PawnTicketSearchTextBox.Text,
                    GetPawnTicketSearchFilter());

            PawnTicketSearchDataGrid.ItemsSource = results;

            PawnTicketSearchResultCountText.Text =
                results.Count == 0
                    ? "ไม่พบรายการ"
                    : $"พบ {results.Count:N0} รายการ";
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            PawnTicketSearchDataGrid.ItemsSource = null;
            PawnTicketSearchResultCountText.Text =
                "ค้นหาไม่สำเร็จ";

            MessageBox.Show(
                $"ไม่สามารถค้นหารายการได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ViewSelectedPawnTicket_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenSelectedPawnTicket();
    }

    private void PawnTicketSearchDataGrid_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        OpenSelectedPawnTicket();
    }

    private void OpenSelectedPawnTicket()
    {
        if (PawnTicketSearchDataGrid.SelectedItem
            is not PawnTicketSearchResult selected)
        {
            MessageBox.Show(
                "กรุณาเลือกรายการที่ต้องการเปิดดู",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        try
        {
            PawnTicketDetail detail =
                _pawnTicketSearchService.GetDetail(
                    selected.Id);

            PawnTicketDetailWindow detailWindow =
                new(detail)
                {
                    Owner = this
                };

            detailWindow.ShowDialog();

            // เมื่อมีการต่อดอก / ไถ่ถอนในหน้ารายละเอียด
            // กลับมาหน้า Search ให้ดึงสถานะล่าสุดจาก SQLite ทันที
            LoadPawnTicketSearchResults();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Handled exception in MainWindow.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดรายละเอียดตั๋วได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowPage(
        UIElement pageToShow,
        Button activeButton,
        string pageTitle,
        string pageSubtitle)
    {
        HomeContent.Visibility = Visibility.Collapsed;
        NewPawnContent.Visibility = Visibility.Collapsed;
        SearchContent.Visibility = Visibility.Collapsed;
        TodayContent.Visibility = Visibility.Collapsed;
        SettingsContent.Visibility = Visibility.Collapsed;

        pageToShow.Visibility = Visibility.Visible;

        HomeButton.Style = (Style)FindResource("SidebarButtonStyle");
        NewPawnButton.Style = (Style)FindResource("SidebarButtonStyle");
        SearchButton.Style = (Style)FindResource("SidebarButtonStyle");
        TodayButton.Style = (Style)FindResource("SidebarButtonStyle");
        SettingsButton.Style = (Style)FindResource("SidebarButtonStyle");

        activeButton.Style = (Style)FindResource("SidebarActiveButtonStyle");

        PageTitleText.Text = pageTitle;
        PageSubtitleText.Text = pageSubtitle;
    }
}
