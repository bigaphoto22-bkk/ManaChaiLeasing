using System.Windows;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class PawnTicketDetailWindow : Window
{
    private readonly PawnTicketSearchService _searchService = new();
    private readonly InterestRenewalService _renewalService = new();
    private readonly RedemptionService _redemptionService = new();
    private readonly SaleService _saleService = new();
    private readonly PawnTicketEditService _editService = new();
    private readonly RepawnService _repawnService = new();
    private readonly int _pawnTicketId;

    public PawnTicketDetailWindow(PawnTicketDetail detail)
    {
        InitializeComponent();

        _pawnTicketId = detail.Id;
        DataContext = detail;
    }

    public RepawnDraft? RepawnDraftRequest { get; private set; }

    private void RepawnItemButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            MessageBoxResult confirmation =
                MessageBox.Show(
                    "สร้างตั๋วจำนำใหม่โดยคัดลอกข้อมูลลูกค้าและสินค้าเดิมหรือไม่\n\n" +
                    "ตั๋วเดิมและประวัติการเงินเดิมจะไม่ถูกแก้ไข",
                    "จำนำสินค้าเดิมอีกครั้ง",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                return;
            }

            RepawnDraftRequest =
                _repawnService.CreateDraft(
                    _pawnTicketId);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not prepare redeemed item for repawn.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถนำสินค้าเดิมมาสร้างตั๋วใหม่ได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void EditTicketButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            PawnTicketEditData editData =
                _editService.GetEditData(
                    _pawnTicketId);

            PawnTicketEditWindow editWindow =
                new(editData)
                {
                    Owner = this
                };

            bool? result =
                editWindow.ShowDialog();

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
                "Could not open controlled pawn ticket edit.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดหน้าแก้ไขข้อมูลได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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

    private void SellButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            SalePreview preview =
                _saleService.GetPreview(
                    _pawnTicketId);

            SaleWindow saleWindow =
                new(preview)
                {
                    Owner = this
                };

            bool? result =
                saleWindow.ShowDialog();

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
                "Could not open sale action.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดหน้าจำหน่ายสินค้าได้\n\n{ex.Message}",
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
