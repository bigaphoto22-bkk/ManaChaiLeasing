using System.Windows;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class PawnTicketDetailWindow : Window
{
    private readonly PawnTicketSearchService _searchService = new();
    private readonly InterestRenewalService _renewalService = new();
    private readonly RedemptionService _redemptionService = new();
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
            AppLog.Error(
                "Could not open interest renewal action.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดหน้าต่อดอกได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void RedeemButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            RedemptionPreview preview =
                _redemptionService.GetPreview(
                    _pawnTicketId);

            RedemptionWindow redemptionWindow =
                new(preview)
                {
                    Owner = this
                };

            bool? result =
                redemptionWindow.ShowDialog();

            if (result == true)
            {
                DataContext =
                    _searchService.GetDetail(
                        _pawnTicketId);
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not open redemption action.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดหน้าไถ่ถอนได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
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
