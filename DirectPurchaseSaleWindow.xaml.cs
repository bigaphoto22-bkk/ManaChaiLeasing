using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class DirectPurchaseSaleWindow : Window
{
    private readonly DirectPurchaseService _service = new();
    private readonly AutomaticBackupService _backupService = new();
    private readonly DirectPurchaseSalePreview _preview;
    private bool _isSaving;

    public DirectPurchaseSaleWindow(DirectPurchaseSalePreview preview)
    {
        InitializeComponent();
        _preview = preview;
        DataContext = preview;

        SaleDatePicker.DisplayDateStart = preview.PurchaseDate.Date;
        SaleDatePicker.DisplayDateEnd = DateTime.Today;

        if (preview.IsEditing)
        {
            HeadingText.Text = "แก้ไขข้อมูลการขาย";
            SaleSectionHeadingText.Text = "ข้อมูลการขายที่ต้องการแก้ไข";
            SaleDatePicker.SelectedDate = preview.SaleDate?.Date ?? DateTime.Today;
            SalePriceTextBox.Text = preview.SalePrice?.ToString("N2") ?? string.Empty;
            SaleNoteTextBox.Text = preview.SaleNote;
            SelectPaymentMethod(preview.SalePaymentMethod);
            EditReasonBorder.Visibility = Visibility.Visible;
            SaveSaleButton.Content = "บันทึกการแก้ไข";
            FooterInfoText.Text = "ระบบจะแก้ Transaction การขายเดิมและคำนวณรายรับกับกำไรใหม่ โดยไม่สร้างยอดขายซ้ำ";
        }
        else
        {
            SaleDatePicker.SelectedDate = DateTime.Today;
        }

        UpdateProfitPreview();
    }

    private void SalePriceTextBox_TextChanged(object sender, TextChangedEventArgs e) =>
        UpdateProfitPreview();

    private void UpdateProfitPreview()
    {
        if (ProfitText is null)
        {
            return;
        }

        if (!TryReadSalePrice(out decimal salePrice) || salePrice <= 0m)
        {
            ProfitText.Text = "-";
            ProfitText.Foreground = Brushes.Gray;
            return;
        }

        decimal profit = salePrice - _preview.PurchasePrice;
        ProfitText.Text = profit >= 0m
            ? $"กำไร {profit:N2} บาท"
            : $"ขาดทุน {Math.Abs(profit):N2} บาท";
        ProfitText.Foreground = profit >= 0m
            ? Brushes.ForestGreen
            : Brushes.Firebrick;
    }

    private void SaveSaleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSaving)
        {
            return;
        }

        _isSaving = true;
        SaveSaleButton.IsEnabled = false;
        SaveSaleButton.Content = "กำลังบันทึก...";

        try
        {
            if (!SaleDatePicker.SelectedDate.HasValue)
                throw new InvalidOperationException("กรุณาเลือกวันที่ขาย");
            if (!TryReadSalePrice(out decimal salePrice) || salePrice <= 0m)
                throw new InvalidOperationException("กรุณากรอกราคาขายให้ถูกต้องและมากกว่า 0 บาท");

            string paymentMethod =
                (PaymentMethodComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()
                ?? string.Empty;
            decimal profit = salePrice - _preview.PurchasePrice;
            string profitText = profit >= 0m
                ? $"กำไร {profit:N2} บาท"
                : $"ขาดทุน {Math.Abs(profit):N2} บาท";

            string actionTitle = _preview.IsEditing
                ? "ยืนยันแก้ไขข้อมูลการขาย"
                : "ยืนยันขายสินค้า";
            string actionDescription = _preview.IsEditing
                ? "ระบบจะอัปเดต Transaction การขายเดิมและคำนวณยอดใหม่"
                : "หลังยืนยัน รายการจะเปลี่ยนเป็นสถานะขายแล้ว";

            MessageBoxResult confirm = MessageBox.Show(
                $"{actionTitle} เลขที่รับซื้อ {_preview.DocumentNumber}\n\n" +
                $"ราคาขาย {salePrice:N2} บาท\n" +
                $"ราคารับซื้อ {_preview.PurchasePrice:N2} บาท\n" +
                $"{profitText}\n\n" +
                actionDescription,
                actionTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            DirectPurchaseSaleResult result = _preview.IsEditing
                ? _service.UpdateSale(
                    _preview.DirectPurchaseId,
                    SaleDatePicker.SelectedDate.Value,
                    salePrice,
                    paymentMethod,
                    SaleNoteTextBox.Text,
                    EditReasonTextBox.Text)
                : _service.SaveSale(
                    _preview.DirectPurchaseId,
                    SaleDatePicker.SelectedDate.Value,
                    salePrice,
                    paymentMethod,
                    SaleNoteTextBox.Text);

            AutomaticBackupExecutionResult backup = _backupService.RunAutomaticBackup();
            string savedProfitText = result.Profit >= 0m
                ? $"กำไร {result.Profit:N2} บาท"
                : $"ขาดทุน {Math.Abs(result.Profit):N2} บาท";
            string backupWarning = backup.IsFailed
                ? $"\n\nหมายเหตุ: Auto Backup ไม่สำเร็จ\n{backup.ErrorMessage}"
                : string.Empty;

            MessageBox.Show(
                (_preview.IsEditing
                    ? "แก้ไขข้อมูลการขายเรียบร้อยแล้ว\n\n"
                    : "บันทึกขายสินค้าเรียบร้อยแล้ว\n\n") +
                $"เลขที่รับซื้อ {result.DocumentNumber}\n" +
                $"รับเข้า {result.SalePrice:N2} บาท\n" +
                savedProfitText + backupWarning,
                AppInfo.StoreName,
                MessageBoxButton.OK,
                backup.IsFailed ? MessageBoxImage.Warning : MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                _preview.IsEditing
                    ? "Direct purchase sale correction failed."
                    : "Direct purchase sale failed.",
                ex);
            MessageBox.Show(
                (_preview.IsEditing
                    ? "ไม่สามารถแก้ไขข้อมูลการขายได้"
                    : "ไม่สามารถบันทึกขายสินค้าได้") +
                $"\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (DialogResult != true)
            {
                _isSaving = false;
                SaveSaleButton.IsEnabled = true;
                SaveSaleButton.Content = _preview.IsEditing
                    ? "บันทึกการแก้ไข"
                    : "ยืนยันขายสินค้า";
            }
        }
    }

    private bool TryReadSalePrice(out decimal amount)
    {
        string text = SalePriceTextBox?.Text?.Trim() ?? string.Empty;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out amount) ||
               decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
    }

    private void SelectPaymentMethod(string value)
    {
        foreach (object item in PaymentMethodComboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem &&
                string.Equals(
                    comboBoxItem.Content?.ToString(),
                    value,
                    StringComparison.Ordinal))
            {
                PaymentMethodComboBox.SelectedItem = comboBoxItem;
                return;
            }
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) =>
        DialogResult = false;
}
