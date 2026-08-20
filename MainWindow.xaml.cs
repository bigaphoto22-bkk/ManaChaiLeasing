using System;
using System.Collections.Generic;
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
    private bool _isInitializing = true;
    private readonly CustomerService _customerService = new();
    private readonly PawnTicketService _pawnTicketService = new();
    private readonly PawnTicketSearchService _pawnTicketSearchService = new();
    private readonly AppSettingService _appSettingService = new();
    private readonly TodaySummaryService _todaySummaryService = new();
    private readonly HomeDashboardService _homeDashboardService = new();
    private readonly DatabaseBackupService _databaseBackupService = new();
    private readonly MachineIdentityService _machineIdentityService = new();
    private int? _selectedCustomerId;

    public MainWindow()
    {
        InitializeComponent();

        InitializeDatabase();
        LoadHomeDashboard();
        LoadSmartLookupValues();

        PawnDatePicker.SelectedDate = DateTime.Today;

        _isInitializing = false;
        UpdateProductForm();
        UpdateAssetPreview();
    }

    private void InitializeDatabase()
    {
        try
        {
            DatabaseInitializer.Initialize();

            DatabaseStatusText.Text = "Offline • SQLite Ready";
            DatabaseStatusText.Foreground = Brushes.ForestGreen;
            DatabaseStatusText.ToolTip = DatabasePaths.DatabaseFile;
        }
        catch (Exception ex)
        {
            DatabaseStatusText.Text = "Database Error";
            DatabaseStatusText.Foreground = Brushes.Firebrick;

            MessageBox.Show(
                $"ไม่สามารถเตรียมฐานข้อมูล SQLite ได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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

    private void LoadHomeDashboard()
    {
        try
        {
            HomeDashboardSummary summary =
                _homeDashboardService.GetSummary();

            HomeDashboardDateText.Text =
                $"วันที่ {summary.SummaryDate:dd/MM/yyyy}";

            HomeDashboardUpdatedText.Text =
                $"อัปเดตล่าสุด {summary.UpdatedAt:HH:mm}";

            HomeActiveTicketCountText.Text =
                $"{summary.ActiveTicketCount:N0} ตั๋ว";

            HomeDueTodayCountText.Text =
                $"{summary.DueTodayCount:N0} ตั๋ว";

            HomeOverdueCountText.Text =
                summary.OverdueCount == 0
                    ? "เกินกำหนด 0 ตั๋ว"
                    : $"เกินกำหนด {summary.OverdueCount:N0} ตั๋ว";

            HomeInterestTodayCountText.Text =
                $"{summary.InterestTodayCount:N0} ครั้ง";

            HomePawnExpenseTodayText.Text =
                $"{summary.PawnExpenseToday:N2} บาท";

            HomeIncomeTodayText.Text =
                $"{summary.IncomeToday:N2} บาท";

            HomeNetCashTodayText.Text =
                $"{summary.NetCashToday:N2} บาท";

            HomeNetCashTodayText.Foreground =
                summary.NetCashToday < 0m
                    ? Brushes.Firebrick
                    : Brushes.ForestGreen;
        }
        catch (Exception ex)
        {
            HomeDashboardUpdatedText.Text =
                "ไม่สามารถโหลด Dashboard ได้";

            MessageBox.Show(
                $"ไม่สามารถโหลดข้อมูลหน้าหลักได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void NewPawnButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            NewPawnContent,
            NewPawnButton,
            "รับจำนำใหม่",
            "บันทึกข้อมูลลูกค้า สินค้า หมายเลขตั๋ว และยอดเงิน");
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
        LoadBusinessSettings();
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
            MachineIdText.Text =
                "ไม่สามารถสร้าง Machine ID ได้";

            MachineFingerprintVersionText.Text =
                ex.Message;
        }
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
            MessageBox.Show(
                $"ไม่สามารถคัดลอกรหัสเครื่องได้\n\n{ex.Message}",
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
            FillCustomerForm(lookupWindow.SelectedCustomer);
        }
    }

    private void SaveCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
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

            MessageBox.Show(
                $"บันทึกข้อมูลลูกค้าเรียบร้อย\n\n{savedCustomer.FirstName} {savedCustomer.LastName}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ไม่สามารถบันทึกข้อมูลลูกค้าได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void FillCustomerForm(Customer customer)
    {
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

    private void SavePawnTicket_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (sender is Button saveButton)
        {
            saveButton.IsEnabled = false;
        }

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
            MessageBox.Show(
                $"ไม่สามารถบันทึกตั๋วจำนำได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (sender is Button saveButtonToEnable)
            {
                saveButtonToEnable.IsEnabled = true;
            }
        }
    }

    private bool TryBuildCustomerInput(out Customer customer)
    {
        customer = new Customer();

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
