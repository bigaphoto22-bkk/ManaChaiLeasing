using System.Windows;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class PawnTicketDetailWindow : Window
{
    private readonly PawnTicketSearchService _searchService = new();
    private readonly InterestRenewalService _renewalService = new();
    private readonly int _pawnTicketId;

    public PawnTicketDetailWindow(PawnTicketDetail detail)
    {
        InitializeComponent();

        _pawnTicketId = detail.Id;
        DataContext = detail;
    }

    private void RenewInterestButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            InterestRenewalPreview preview =
                _renewalService.GetPreview(
                    _pawnTicketId);

            InterestRenewalWindow renewalWindow =
                new(preview)
                {
                    Owner = this
                };

            bool? result =
                renewalWindow.ShowDialog();

            if (result == true)
            {
                DataContext =
                    _searchService.GetDetail(
                        _pawnTicketId);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ไม่สามารถเปิดหน้าต่อดอกได้\n\n{ex.Message}",
                "มานะชัย ลิสซิ่ง",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void CloseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Close();
    }
}
