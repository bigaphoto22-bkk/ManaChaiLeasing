using System.Windows;
using System.Windows.Controls;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class InterestRenewalWindow : Window
{
    private readonly InterestRenewalService _service = new();
    private readonly InterestRenewalPreview _preview;

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

        SaveInterestButton.IsEnabled = false;

        try
        {
            SavedResult = _service.SaveRenewal(
                _preview.PawnTicketId,
                paymentMethod,
                RenewalNoteTextBox.Text);

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
            MessageBox.Show(
                $"ไม่สามารถบันทึกการต่อดอกได้\n\n{ex.Message}",
                "มานะชัย ลิสซิ่ง",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            SaveInterestButton.IsEnabled = true;
        }
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
