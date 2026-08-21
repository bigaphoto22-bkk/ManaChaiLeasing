using System.Windows;
using System.Windows.Controls;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class InterestRenewalWindow : Window
{
    private readonly InterestRenewalService _service = new();
    private readonly AutomaticBackupService _automaticBackupService = new();
    private readonly InterestRenewalPreview _preview;

    private bool _isSaving;

    public InterestRenewalResult? SavedResult { get; private set; }

    public InterestRenewalWindow(
        InterestRenewalPreview preview)
    {
        InitializeComponent();

        _preview = preview;
        DataContext = preview;
    }

    private void SaveInterestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isSaving)
        {
            AppLog.Warning(
                "Duplicate interest-renewal save action blocked at UI.");

            return;
        }

        _isSaving = true;
        SaveInterestButton.IsEnabled = false;
        SaveInterestButton.Content = "กำลังบันทึก...";

        try
        {
            string paymentMethod =
                (PaymentMethodComboBox.SelectedItem as ComboBoxItem)
                    ?.Content
                    ?.ToString()
                ?? string.Empty;

            MessageBoxResult confirm = MessageBox.Show(
                $"ยืนยันต่อดอก {_preview.InterestSequenceText}\n\n" +
                $"รับเงิน {_preview.InterestAmount:N2} บาท\n" +
                $"ครบกำหนดใหม่ {_preview.NewDueDate:dd/MM/yyyy}",
                "ยืนยันต่อดอก",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            SavedResult = _service.SaveRenewal(
                _preview.PawnTicketId,
                _preview.InterestSequence,
                paymentMethod,
                RenewalNoteTextBox.Text);

            AutomaticBackupExecutionResult backupResult =
                _automaticBackupService.RunAutomaticBackup();

            if (backupResult.IsFailed)
            {
                MessageBox.Show(
                    "บันทึกต่อดอกเรียบร้อยแล้ว แต่ Auto Backup ไม่สำเร็จ\n\n" +
                    $"{backupResult.ErrorMessage}\n\n" +
                    "กรุณาตรวจ Drive สำรองข้อมูลที่หน้า ตั้งค่า",
                    "Auto Backup ไม่สำเร็จ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            InterestRenewalSuccessWindow successWindow =
                new(SavedResult)
                {
                    Owner = this
                };

            successWindow.ShowDialog();

            DialogResult = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Interest renewal save failed.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถบันทึกการต่อดอกได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            if (DialogResult != true)
            {
                _isSaving = false;
                SaveInterestButton.IsEnabled = true;
                SaveInterestButton.Content = "ยืนยันต่อดอก";
            }
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
