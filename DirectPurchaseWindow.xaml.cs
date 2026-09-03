using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ManaChaiLeasing.Models;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class DirectPurchaseWindow : Window
{
    private readonly DirectPurchaseService _service = new();
    private readonly AutomaticBackupService _backupService = new();

    public bool DataChanged { get; private set; }

    public DirectPurchaseWindow()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            LoadRows();
            SearchTextBox.Focus();
        };
    }

    private void LoadRows()
    {
        try
        {
            DirectPurchaseStatus? status = GetSelectedStatus();
            List<DirectPurchaseListRow> rows = _service.Search(SearchTextBox.Text, status);
            PurchaseGrid.ItemsSource = rows;
            ResultCountText.Text = rows.Count == 0 ? "ไม่พบรายการ" : $"พบ {rows.Count:N0} รายการ";
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not load direct purchases.", ex);
            MessageBox.Show($"ไม่สามารถโหลดรายการซื้อขายได้\n\n{ex.Message}", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private DirectPurchaseStatus? GetSelectedStatus()
    {
        string tag = (StatusComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "All";
        return Enum.TryParse(tag, out DirectPurchaseStatus status) ? status : null;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded) LoadRows();
    }
    private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) LoadRows();
    }
    private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadRows();

    private void NewPurchaseButton_Click(object sender, RoutedEventArgs e)
    {
        DirectPurchaseEditWindow window = new() { Owner = this };
        if (window.ShowDialog() == true)
        {
            DataChanged = true;
            LoadRows();
        }
    }

    private void OpenPurchaseButton_Click(object sender, RoutedEventArgs e) => OpenSelected();
    private void PurchaseGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();

    private void OpenSelected()
    {
        if (PurchaseGrid.SelectedItem is not DirectPurchaseListRow row)
        {
            MessageBox.Show("กรุณาเลือกรายการก่อน", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DirectPurchaseEditWindow window = new(row.Id) { Owner = this };
        if (window.ShowDialog() == true)
        {
            DataChanged = true;
            LoadRows();
        }
    }

    private void CancelPurchaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (PurchaseGrid.SelectedItem is not DirectPurchaseListRow row)
        {
            MessageBox.Show("กรุณาเลือกรายการที่ต้องการยกเลิก", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (row.Status != DirectPurchaseStatus.InStock)
        {
            MessageBox.Show("ยกเลิกได้เฉพาะรายการที่มีสถานะรอขายเท่านั้น", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        DirectPurchaseCancelWindow window = new(row.DocumentNumber, row.ProductSummary) { Owner = this };
        if (window.ShowDialog() != true) return;

        try
        {
            _service.Cancel(row.Id, window.CancellationReason);
            DataChanged = true;
            LoadRows();
            AutomaticBackupExecutionResult backup =
                _backupService.RunAutomaticBackup();

            MessageBox.Show(
                "ยกเลิกรายการรับซื้อเรียบร้อยแล้ว" +
                (backup.IsFailed
                    ? $"\n\nหมายเหตุ: Auto Backup ไม่สำเร็จ\n{backup.ErrorMessage}"
                    : string.Empty),
                AppInfo.StoreName,
                MessageBoxButton.OK,
                backup.IsFailed
                    ? MessageBoxImage.Warning
                    : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not cancel direct purchase.", ex);
            MessageBox.Show($"ไม่สามารถยกเลิกรายการได้\n\n{ex.Message}", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
