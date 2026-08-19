using System.Windows;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class RedemptionSuccessWindow : Window
{
    public RedemptionSuccessWindow(
        RedemptionResult result)
    {
        InitializeComponent();

        TicketNumberText.Text =
            result.TicketNumber;

        PrincipalText.Text =
            $"{result.PrincipalAmount:N2} บาท";

        InterestText.Text =
            $"{result.FinalInterestAmount:N2} บาท";

        TotalText.Text =
            $"{result.RedemptionTotal:N2} บาท";

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
