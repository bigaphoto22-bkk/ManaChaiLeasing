using System.Windows;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class PawnTicketDetailWindow : Window
{
    public PawnTicketDetailWindow(PawnTicketDetail detail)
    {
        InitializeComponent();
        DataContext = detail;
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
