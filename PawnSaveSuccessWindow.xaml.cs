using System.Windows;

namespace ManaChaiLeasing;

public partial class PawnSaveSuccessWindow : Window
{
    public PawnSaveSuccessWindow(
        string ticketNumber,
        decimal principalAmount)
    {
        InitializeComponent();

        TicketNumberText.Text = ticketNumber;
        AmountText.Text = $"{principalAmount:N2} บาท";
        CashFlowText.Text = $"จ่ายออก {principalAmount:N2} บาท";
    }

    private void OkButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
