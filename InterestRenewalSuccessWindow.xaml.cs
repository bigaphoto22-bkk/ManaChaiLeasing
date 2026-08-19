using System.Windows;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class InterestRenewalSuccessWindow : Window
{
    public InterestRenewalSuccessWindow(
        InterestRenewalResult result)
    {
        InitializeComponent();

        TicketNumberText.Text =
            result.TicketNumber;

        SequenceText.Text =
            $"ต่อดอกครั้งที่ {result.InterestSequence:N0}";

        AmountText.Text =
            $"{result.InterestAmount:N2} บาท";

        NewDueDateText.Text =
            result.NewDueDate.ToString("dd/MM/yyyy");

        PaymentMethodText.Text =
            result.PaymentMethod;
    }

    private void OkButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
