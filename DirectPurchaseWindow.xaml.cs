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
            UpdateActionButtons();
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

    private void EditPurchaseButton_Click(object sender, RoutedEventArgs e) => EditSelected();
    private void PurchaseGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();
    private void PurchaseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateActionButtons();

    private void EditSelected()
    {
        if (PurchaseGrid.SelectedItem is not DirectPurchaseListRow row)
        {
            MessageBox.Show("กรุณาเลือกรายการก่อน", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (row.Status != DirectPurchaseStatus.Sold)
        {
            OpenSelected();
            return;
        }

        try
        {
            DirectPurchaseSalePreview preview = _service.GetSaleEditPreview(row.Id);
            DirectPurchaseSaleWindow window = new(preview) { Owner = this };
            if (window.ShowDialog() == true)
            {
                DataChanged = true;
                LoadRows();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not open direct purchase sale correction.", ex);
            MessageBox.Show($"ไม่สามารถเปิดแก้ไขข้อมูลการขายได้\n\n{ex.Message}", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateActionButtons()
    {
        if (CancelPurchaseButton is null || SellPurchaseButton is null || EditPurchaseButton is null)
        {
            return;
        }

        if (PurchaseGrid.SelectedItem is not DirectPurchaseListRow row)
        {
            CancelPurchaseButton.Visibility = Visibility.Visible;
            SellPurchaseButton.Visibility = Visibility.Visible;
            CancelPurchaseButton.IsEnabled = false;
            SellPurchaseButton.IsEnabled = false;
            EditPurchaseButton.IsEnabled = false;
            EditPurchaseButton.Content = "แก้ไขรายละเอียด";
            return;
        }

        CancelPurchaseButton.IsEnabled = true;
        SellPurchaseButton.IsEnabled = true;
        EditPurchaseButton.IsEnabled = true;

        switch (row.Status)
        {
            case DirectPurchaseStatus.InStock:
                CancelPurchaseButton.Visibility = Visibility.Visible;
                SellPurchaseButton.Visibility = Visibility.Visible;
                EditPurchaseButton.Content = "แก้ไขข้อมูลรับซื้อ";
                break;

            case DirectPurchaseStatus.Sold:
                CancelPurchaseButton.Visibility = Visibility.Collapsed;
                SellPurchaseButton.Visibility = Visibility.Collapsed;
                EditPurchaseButton.Content = "แก้ไขข้อมูลการขาย";
                break;

            default:
                CancelPurchaseButton.Visibility = Visibility.Collapsed;
                SellPurchaseButton.Visibility = Visibility.Collapsed;
                EditPurchaseButton.Content = "ดูรายละเอียด";
                break;
        }
    }

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

    private void SellPurchaseButton_Click(object sender, RoutedEventArgs e)
    {
        if (PurchaseGrid.SelectedItem is not DirectPurchaseListRow row)
        {
            MessageBox.Show("กรุณาเลือกรายการที่ต้องการขาย", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (row.Status != DirectPurchaseStatus.InStock)
        {
            MessageBox.Show("ขายได้เฉพาะรายการที่มีสถานะรอขายเท่านั้น", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            DirectPurchaseSalePreview preview = _service.GetSalePreview(row.Id);
            DirectPurchaseSaleWindow window = new(preview) { Owner = this };
            if (window.ShowDialog() == true)
            {
                DataChanged = true;
                LoadRows();
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not open direct purchase sale or correction.", ex);
            MessageBox.Show($"ไม่สามารถเปิดข้อมูลการขายได้\n\n{ex.Message}", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
