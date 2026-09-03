using System.Windows;

namespace ManaChaiLeasing;

public partial class DirectPurchaseCancelWindow : Window
{
    public string CancellationReason => ReasonTextBox.Text.Trim();

    public DirectPurchaseCancelWindow(string documentNumber, string productSummary)
    {
        InitializeComponent();
        SummaryText.Text = $"เลขที่รับซื้อ: {documentNumber}\nสินค้า: {productSummary}";
        Loaded += (_, _) => ReasonTextBox.Focus();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CancellationReason))
        {
            MessageBox.Show("กรุณาระบุเหตุผลการยกเลิก", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show("ยืนยันยกเลิกรายการรับซื้อนี้หรือไม่?", "ยืนยันการยกเลิก", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            DialogResult = true;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
