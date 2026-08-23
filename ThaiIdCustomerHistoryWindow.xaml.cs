using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class ThaiIdCustomerHistoryWindow : Window
{
    private readonly int _customerId;
    private readonly ThaiIdCustomerHistoryService _historyService = new();
    private readonly PawnTicketSearchService _ticketSearchService = new();

    public ThaiIdCustomerHistoryWindow(int customerId)
    {
        InitializeComponent();

        _customerId = customerId;
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
    }

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

    private void HistoryDataGrid_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        OpenSelectedTicketButton.IsEnabled =
            HistoryDataGrid.SelectedItem is
                ThaiIdCustomerHistoryRow;
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

    private void UseCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
