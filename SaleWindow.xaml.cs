using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class SaleWindow : Window
{
    private readonly SaleService _service = new();
    private readonly AutomaticBackupService _automaticBackupService = new();
    private readonly SalePreview _preview;

    private bool _isSaving;

    public SaleResult? SavedResult { get; private set; }

    public SaleWindow(SalePreview preview)
    {
        InitializeComponent();

        _preview = preview;
        DataContext = preview;

        SaleDatePicker.SelectedDate = DateTime.Today;
        SaleDatePicker.DisplayDateEnd = DateTime.Today;
        UpdateProfitPreview();
    }

    private void SaleAmountTextBox_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        UpdateProfitPreview();
    }

    private void UpdateProfitPreview()
    {
        if (SaleProfitText is null)
        {
            return;
        }

        if (!TryReadSaleAmount(out decimal saleAmount) ||
            saleAmount <= 0m)
        {
            SaleProfitText.Text = "-";
            SaleProfitText.Foreground = Brushes.Gray;
            return;
        }

        decimal profit =
            saleAmount - _preview.PrincipalAmount;

        if (profit >= 0m)
        {
            SaleProfitText.Text =
                $"กำไร {profit:N2} บาท";
            SaleProfitText.Foreground =
                Brushes.ForestGreen;
        }
        else
        {
            SaleProfitText.Text =
                $"ขาดทุน {Math.Abs(profit):N2} บาท";
            SaleProfitText.Foreground =
                Brushes.Firebrick;
        }
    }

    private void SaveSaleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isSaving)
        {
            AppLog.Warning(
                "Duplicate sale save action blocked at UI.");
            return;
        }

        _isSaving = true;
        SaveSaleButton.IsEnabled = false;
        SaveSaleButton.Content = "กำลังบันทึก...";

        try
        {
            if (!SaleDatePicker.SelectedDate.HasValue)
            {
                throw new InvalidOperationException(
                    "กรุณาเลือกวันที่จำหน่าย");
            }

            if (!TryReadSaleAmount(out decimal saleAmount) ||
                saleAmount <= 0m)
            {
                throw new InvalidOperationException(
                    "กรุณากรอกราคาจำหน่ายให้ถูกต้องและมากกว่า 0 บาท");
            }

            string paymentMethod =
                (PaymentMethodComboBox.SelectedItem as ComboBoxItem)
                    ?.Content
                    ?.ToString()
                ?? string.Empty;

            decimal profit =
                saleAmount - _preview.PrincipalAmount;

            string profitText = profit >= 0m
                ? $"กำไร {profit:N2} บาท"
                : $"ขาดทุน {Math.Abs(profit):N2} บาท";

            MessageBoxResult confirm = MessageBox.Show(
                $"ยืนยันจำหน่ายเลขตั๋ว {_preview.TicketNumber}\n\n" +
                $"ราคาจำหน่าย {saleAmount:N2} บาท\n" +
                $"เงินต้น {_preview.PrincipalAmount:N2} บาท\n" +
                $"{profitText}\n\n" +
                "หลังยืนยัน ตั๋วจะเปลี่ยนเป็นสถานะจำหน่ายแล้ว",
                "ยืนยันจำหน่ายสินค้า",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            SavedResult = _service.SaveSale(
                _preview.PawnTicketId,
                _preview.InterestRenewalCount,
                SaleDatePicker.SelectedDate.Value,
                saleAmount,
                paymentMethod,
                SaleNoteTextBox.Text);

            AutomaticBackupExecutionResult backupResult =
                _automaticBackupService.RunAutomaticBackup();

            if (backupResult.IsFailed)
            {
                MessageBox.Show(
                    "บันทึกจำหน่ายเรียบร้อยแล้ว แต่ Auto Backup ไม่สำเร็จ\n\n" +
                    $"{backupResult.ErrorMessage}\n\n" +
                    "กรุณาตรวจ Drive สำรองข้อมูลที่หน้า ตั้งค่า",
                    "Auto Backup ไม่สำเร็จ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            string savedProfitText = SavedResult.Profit >= 0m
                ? $"กำไร {SavedResult.Profit:N2} บาท"
                : $"ขาดทุน {Math.Abs(SavedResult.Profit):N2} บาท";

            MessageBox.Show(
                "บันทึกจำหน่ายสินค้าเรียบร้อยแล้ว\n\n" +
                $"เลขตั๋ว {SavedResult.TicketNumber}\n" +
                $"รับเข้า {SavedResult.SaleAmount:N2} บาท\n" +
                savedProfitText,
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Sale save failed.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถบันทึกการจำหน่ายได้\n\n{ex.Message}",
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
                SaveSaleButton.Content = "ยืนยันจำหน่าย";
            }
        }
    }

    private bool TryReadSaleAmount(out decimal amount)
    {
        string text = SaleAmountTextBox?.Text?.Trim()
            ?? string.Empty;

        return decimal.TryParse(
                   text,
                   NumberStyles.Number,
                   CultureInfo.CurrentCulture,
                   out amount) ||
               decimal.TryParse(
                   text,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out amount);
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
