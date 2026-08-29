using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class ThaiIdCustomerHistoryWindow : Window
{
    private enum CustomerProfitPeriodPreset
    {
        All,
        Today,
        ThisMonth,
        ThisYear,
        Custom
    }

    private readonly int _customerId;
    private readonly ThaiIdCustomerHistoryService _historyService = new();
    private readonly PawnTicketSearchService _ticketSearchService = new();
    private readonly RepawnService _repawnService = new();
    private readonly CustomerProfitSummaryService _customerProfitService = new();
    private bool _isInitializingCustomerProfitPeriod = true;

    public ThaiIdCustomerHistoryWindow(int customerId)
    {
        _customerId = customerId;

        InitializeComponent();

        DateTime today = DateTime.Today;

        CustomerProfitCustomStartDatePicker.SelectedDate =
            new DateTime(
                today.Year,
                today.Month,
                1);

        CustomerProfitCustomEndDatePicker.SelectedDate =
            today;

        CustomerProfitPeriodComboBox.SelectedIndex =
            (int)CustomerProfitPeriodPreset.All;

        _isInitializingCustomerProfitPeriod = false;

        LoadHistory();
    }

    private void LoadHistory()
    {
        ThaiIdCustomerHistorySummary summary =
            _historyService.GetHistory(
                _customerId);

        DataContext = summary;
        HistoryDataGrid.ItemsSource = summary.Tickets;

        HistoryCountText.Text =
            summary.TotalTicketCount == 0
                ? "ยังไม่มีรายการ"
                : $"ทั้งหมด {summary.TotalTicketCount:N0} ตั๋ว";

        EmptyHistoryText.Visibility =
            summary.TotalTicketCount == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        HistoryDataGrid.Visibility =
            summary.TotalTicketCount == 0
                ? Visibility.Collapsed
                : Visibility.Visible;

        ConfigureAlert(summary.AlertLevel);

        OpenSelectedTicketButton.IsEnabled = false;
        RepawnSelectedTicketButton.IsEnabled = false;
        HistoryActionHintText.Text =
            "เลือกตั๋วเพื่อเปิดดูรายละเอียด";

        RefreshCustomerProfitSummary(
            applyCustomPeriod: true);
    }

    public RepawnDraft? RepawnDraftRequest { get; private set; }

    private void ConfigureAlert(
        ThaiIdCustomerHistoryAlertLevel level)
    {
        switch (level)
        {
            case ThaiIdCustomerHistoryAlertLevel.Overdue:
                HistoryAlertBorder.Background =
                    new SolidColorBrush(Color.FromRgb(254, 242, 242));
                HistoryAlertBorder.BorderBrush =
                    new SolidColorBrush(Color.FromRgb(254, 202, 202));
                HistoryAlertText.Foreground =
                    Brushes.Firebrick;
                break;

            case ThaiIdCustomerHistoryAlertLevel.DueToday:
                HistoryAlertBorder.Background =
                    new SolidColorBrush(Color.FromRgb(255, 251, 235));
                HistoryAlertBorder.BorderBrush =
                    new SolidColorBrush(Color.FromRgb(253, 230, 138));
                HistoryAlertText.Foreground =
                    Brushes.DarkOrange;
                break;

            case ThaiIdCustomerHistoryAlertLevel.Active:
                HistoryAlertBorder.Background =
                    new SolidColorBrush(Color.FromRgb(239, 246, 255));
                HistoryAlertBorder.BorderBrush =
                    new SolidColorBrush(Color.FromRgb(191, 219, 254));
                HistoryAlertText.Foreground =
                    Brushes.RoyalBlue;
                break;

            default:
                HistoryAlertBorder.Background =
                    new SolidColorBrush(Color.FromRgb(236, 253, 245));
                HistoryAlertBorder.BorderBrush =
                    new SolidColorBrush(Color.FromRgb(167, 243, 208));
                HistoryAlertText.Foreground =
                    Brushes.ForestGreen;
                break;
        }
    }

    private void CustomerProfitPeriodComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializingCustomerProfitPeriod)
        {
            return;
        }

        bool isCustom =
            CustomerProfitPeriodComboBox.SelectedIndex ==
            (int)CustomerProfitPeriodPreset.Custom;

        CustomerProfitCustomPeriodPanel.Visibility =
            isCustom
                ? Visibility.Visible
                : Visibility.Collapsed;

        if (isCustom)
        {
            CustomerProfitPeriodText.Text =
                "ช่วงเวลา: เลือกวันที่แล้วกดแสดงผล";
            return;
        }

        RefreshCustomerProfitSummary(
            applyCustomPeriod: false);
    }

    private void ApplyCustomerProfitCustomPeriodButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        RefreshCustomerProfitSummary(
            applyCustomPeriod: true);
    }

    private void RefreshCustomerProfitSummary(
        bool applyCustomPeriod)
    {
        try
        {
            DateTime today = DateTime.Today;
            DateTime? startDate = null;
            DateTime? endDate = null;
            string periodText;

            CustomerProfitPeriodPreset preset =
                CustomerProfitPeriodComboBox.SelectedIndex switch
                {
                    1 => CustomerProfitPeriodPreset.Today,
                    2 => CustomerProfitPeriodPreset.ThisMonth,
                    3 => CustomerProfitPeriodPreset.ThisYear,
                    4 => CustomerProfitPeriodPreset.Custom,
                    _ => CustomerProfitPeriodPreset.All
                };

            switch (preset)
            {
                case CustomerProfitPeriodPreset.Today:
                    startDate = today;
                    endDate = today;
                    periodText =
                        $"วันที่ {today:dd/MM/yyyy}";
                    break;

                case CustomerProfitPeriodPreset.ThisMonth:
                    startDate = new DateTime(
                        today.Year,
                        today.Month,
                        1);
                    endDate = today;
                    periodText =
                        $"เดือน {today:MM/yyyy}";
                    break;

                case CustomerProfitPeriodPreset.ThisYear:
                    startDate = new DateTime(
                        today.Year,
                        1,
                        1);
                    endDate = today;
                    periodText =
                        $"ปี {today:yyyy}";
                    break;

                case CustomerProfitPeriodPreset.Custom:
                    CustomerProfitCustomPeriodPanel.Visibility =
                        Visibility.Visible;

                    if (!applyCustomPeriod)
                    {
                        return;
                    }

                    if (!CustomerProfitCustomStartDatePicker
                            .SelectedDate.HasValue ||
                        !CustomerProfitCustomEndDatePicker
                            .SelectedDate.HasValue)
                    {
                        MessageBox.Show(
                            "กรุณาเลือกวันที่เริ่มต้นและวันที่สิ้นสุด",
                            AppInfo.StoreName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    startDate = CustomerProfitCustomStartDatePicker
                        .SelectedDate.Value.Date;

                    endDate = CustomerProfitCustomEndDatePicker
                        .SelectedDate.Value.Date;

                    if (startDate.Value > endDate.Value)
                    {
                        MessageBox.Show(
                            "วันที่เริ่มต้นต้องไม่มากกว่าวันที่สิ้นสุด",
                            AppInfo.StoreName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        return;
                    }

                    periodText =
                        $"{startDate.Value:dd/MM/yyyy} - " +
                        $"{endDate.Value:dd/MM/yyyy}";
                    break;

                default:
                    CustomerProfitCustomPeriodPanel.Visibility =
                        Visibility.Collapsed;
                    periodText = "ทั้งหมด";
                    break;
            }

            CustomerProfitSummary summary =
                _customerProfitService.GetSummary(
                    _customerId,
                    startDate,
                    endDate);

            CustomerProfitPeriodText.Text =
                $"ช่วงเวลา: {periodText}";

            CustomerInterestIncomeText.Text =
                $"{summary.InterestIncome:N2} บาท";

            CustomerRedemptionProfitText.Text =
                $"{summary.RedemptionProfit:N2} บาท";

            CustomerSaleProfitText.Text =
                $"{summary.SaleProfit:N2} บาท";

            CustomerTotalProfitText.Text =
                $"{summary.Profit:N2} บาท";

            CustomerSaleProfitText.Foreground =
                summary.SaleProfit < 0m
                    ? Brushes.Firebrick
                    : Brushes.ForestGreen;

            CustomerTotalProfitText.Foreground =
                summary.Profit < 0m
                    ? Brushes.Firebrick
                    : Brushes.ForestGreen;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not load customer profit summary.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถคำนวณกำไรจากลูกค้ารายนี้ได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void HistoryDataGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (HistoryDataGrid.SelectedItem is not
            ThaiIdCustomerHistoryRow selected)
        {
            OpenSelectedTicketButton.IsEnabled = false;
            RepawnSelectedTicketButton.IsEnabled = false;
            HistoryActionHintText.Text =
                "เลือกตั๋วเพื่อเปิดดูรายละเอียด";
            return;
        }

        OpenSelectedTicketButton.IsEnabled = true;
        RepawnSelectedTicketButton.IsEnabled =
            selected.CanRepawn;

        if (selected.CanRepawn)
        {
            HistoryActionHintText.Text =
                "ตั๋วนี้ไถ่ถอนแล้ว สามารถนำสินค้าเดิมมาสร้างตั๋วใหม่ได้";

            RepawnSelectedTicketButton.ToolTip =
                "พร้อมสร้างตั๋วจำนำใหม่จากข้อมูลสินค้าเดิม";
        }
        else if (selected.HasRepawnTicket)
        {
            HistoryActionHintText.Text =
                "สินค้าจากตั๋วนี้ถูกนำกลับมาจำนำใหม่แล้ว";

            RepawnSelectedTicketButton.ToolTip =
                "ตั๋วเดิมหนึ่งใบใช้สร้างตั๋วใหม่ได้เพียงครั้งเดียว";
        }
        else
        {
            HistoryActionHintText.Text =
                "จำนำสินค้าเดิมได้เมื่อตั๋วอยู่ในสถานะไถ่ถอนแล้วเท่านั้น";

            RepawnSelectedTicketButton.ToolTip =
                "ตั๋วนี้ยังไม่อยู่ในสถานะไถ่ถอนแล้ว";
        }
    }

    private void HistoryDataGrid_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        OpenSelectedTicket();
    }

    private void OpenSelectedTicketButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenSelectedTicket();
    }

    private void OpenSelectedTicket()
    {
        if (HistoryDataGrid.SelectedItem is not
            ThaiIdCustomerHistoryRow selected)
        {
            return;
        }

        try
        {
            PawnTicketDetail detail =
                _ticketSearchService.GetDetail(
                    selected.PawnTicketId);

            PawnTicketDetailWindow detailWindow =
                new(detail)
                {
                    Owner = this
                };

            detailWindow.ShowDialog();

            if (detailWindow.RepawnDraftRequest is not null)
            {
                RepawnDraftRequest =
                    detailWindow.RepawnDraftRequest;

                DialogResult = true;
                return;
            }

            // ต่อดอก ไถ่ถอน หรือจำหน่ายจากหน้ารายละเอียดได้
            // เมื่อกลับมาให้แสดงสถานะและประวัติล่าสุดทันที
            LoadHistory();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not open ticket from Thai ID customer history.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดรายละเอียดตั๋วได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RepawnSelectedTicketButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (HistoryDataGrid.SelectedItem is not
            ThaiIdCustomerHistoryRow selected ||
            !selected.CanRepawn)
        {
            return;
        }

        try
        {
            MessageBoxResult confirmation =
                MessageBox.Show(
                    $"สร้างตั๋วจำนำใหม่จากตั๋ว {selected.TicketNumber} หรือไม่\n\n" +
                    "ระบบจะคัดลอกเฉพาะข้อมูลลูกค้าและสินค้า",
                    "จำนำสินค้าเดิมอีกครั้ง",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            RepawnDraftRequest =
                _repawnService.CreateDraft(
                    selected.PawnTicketId);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not prepare repawn from Thai ID customer history.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถนำสินค้าเดิมมาสร้างตั๋วใหม่ได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void UseCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
