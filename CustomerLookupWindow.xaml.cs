using System.Windows;
using System.Windows.Input;
using ManaChaiLeasing.Models;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class CustomerLookupWindow : Window
{
    private readonly CustomerService _customerService = new();

    public Customer? SelectedCustomer { get; private set; }

    public RepawnDraft? RepawnDraftRequest { get; private set; }

    public CustomerLookupWindow()
    {
        InitializeComponent();

        SearchTextBox.TextChanged += SearchTextBox_TextChanged;

        LoadCustomers();
        SearchTextBox.Focus();
    }

    private void SearchTextBox_TextChanged(
        object sender,
        System.Windows.Controls.TextChangedEventArgs e)
    {
        LoadCustomers();
    }

    private void LoadCustomers()
    {
        try
        {
            List<Customer> customers =
                _customerService.SearchCustomers(SearchTextBox.Text);

            CustomerGrid.ItemsSource = customers;

            ResultCountText.Text = customers.Count == 0
                ? "ไม่พบข้อมูลลูกค้า"
                : $"พบ {customers.Count:N0} รายการ";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ไม่สามารถค้นหาข้อมูลลูกค้าได้\n\n{ex.Message}",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void SelectCustomerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        SelectCurrentCustomer();
    }

    private void CustomerGrid_MouseDoubleClick(
        object sender,
        MouseButtonEventArgs e)
    {
        SelectCurrentCustomer();
    }

    private void ViewCustomerHistoryButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (CustomerGrid.SelectedItem is not Customer customer)
        {
            MessageBox.Show(
                "กรุณาเลือกลูกค้าที่ต้องการดูประวัติก่อน",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        try
        {
            ThaiIdCustomerHistoryWindow historyWindow =
                new(customer.Id)
                {
                    Owner = this
                };

            bool? result =
                historyWindow.ShowDialog();

            if (historyWindow.RepawnDraftRequest is not null)
            {
                RepawnDraftRequest =
                    historyWindow.RepawnDraftRequest;

                DialogResult = true;
                return;
            }

            if (result == true)
            {
                SelectedCustomer = customer;
                DialogResult = true;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not open customer history from customer lookup.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดประวัติลูกค้าได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void SelectCurrentCustomer()
    {
        if (CustomerGrid.SelectedItem is not Customer customer)
        {
            MessageBox.Show(
                "กรุณาเลือกลูกค้าที่ต้องการก่อน",
                ManaChaiLeasing.AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            return;
        }

        SelectedCustomer = customer;
        DialogResult = true;
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
