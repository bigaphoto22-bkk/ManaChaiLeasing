using System.Windows;
using System.Windows.Controls;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class RedemptionWindow : Window
{
    private readonly RedemptionService _service = new();
    private readonly AutomaticBackupService _automaticBackupService = new();
    private readonly RedemptionPreview _preview;

    public RedemptionResult? SavedResult { get; private set; }

    public RedemptionWindow(
        RedemptionPreview preview)
    {
        InitializeComponent();

        _preview = preview;
        DataContext = preview;
    }

    private void SaveRedemptionButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        string paymentMethod =
            (PaymentMethodComboBox.SelectedItem as ComboBoxItem)
                ?.Content
                ?.ToString()
            ?? string.Empty;

        MessageBoxResult confirm = MessageBox.Show(
            $"ยืนยันไถ่ถอนเลขตั๋ว {_preview.TicketNumber}\n\n" +
            $"เงินต้น {_preview.PrincipalAmount:N2} บาท\n" +
            $"ดอกเบี้ยรอบสุดท้าย {_preview.FinalInterestAmount:N2} บาท\n" +
            $"รับชำระทั้งหมด {_preview.RedemptionTotal:N2} บาท\n\n" +
            "หลังยืนยัน ตั๋วจะเปลี่ยนเป็นสถานะไถ่ถอนแล้ว",
            "ยืนยันไถ่ถอน",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        SaveRedemptionButton.IsEnabled = false;

        try
        {
            SavedResult = _service.SaveRedemption(
                _preview.PawnTicketId,
                paymentMethod,
                RedemptionNoteTextBox.Text);

            AutomaticBackupExecutionResult backupResult =
                _automaticBackupService.RunAutomaticBackup();

            if (backupResult.IsFailed)
            {
                MessageBox.Show(
                    "บันทึกไถ่ถอนเรียบร้อยแล้ว แต่ Auto Backup ไม่สำเร็จ\n\n" +
                    $"{backupResult.ErrorMessage}\n\n" +
                    "กรุณาตรวจ Drive สำรองข้อมูลที่หน้า ตั้งค่า",
                    "Auto Backup ไม่สำเร็จ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            RedemptionSuccessWindow successWindow =
                new(SavedResult)
                {
                    Owner = this
                };

            successWindow.ShowDialog();

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ไม่สามารถบันทึกการไถ่ถอนได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SaveRedemptionButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
