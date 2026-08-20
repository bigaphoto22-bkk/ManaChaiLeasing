using System.Windows;
using Microsoft.Win32;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class LicenseActivationWindow : Window
{
    private readonly MachineIdentityService _machineIdentityService = new();

    private readonly LicenseValidationService _licenseValidationService = new();

    public LicenseActivationWindow(
        LicenseValidationResult currentResult)
    {
        InitializeComponent();

        MachineIdentity identity =
            _machineIdentityService.GetIdentity();

        MachineIdText.Text =
            identity.MachineId;

        ShowValidationResult(
            currentResult);
    }

    private void ShowValidationResult(
        LicenseValidationResult result)
    {
        LicenseStatusText.Text =
            result.Message;

        if (result.Status ==
            LicenseValidationStatus.PublicKeyNotConfigured)
        {
            LicenseDetailText.Text =
                "Vendor Public Key ยังไม่ได้ถูกฝังในโปรแกรมชุดนี้ " +
                "กรุณาติดต่อผู้จำหน่าย";
            return;
        }

        if (result.Status ==
            LicenseValidationStatus.Expired &&
            result.Payload is not null)
        {
            LicenseDetailText.Text =
                $"ลูกค้า: {result.Payload.CustomerName} • " +
                $"หมดอายุ: {result.ExpiryText}";
            return;
        }

        if (result.Status ==
            LicenseValidationStatus.WrongMachine &&
            result.Payload is not null)
        {
            LicenseDetailText.Text =
                $"License ออกให้ Machine ID: {result.Payload.MachineId}";
            return;
        }

        LicenseDetailText.Text =
            $"ตำแหน่ง License: {LicensePaths.LicenseFile}";
    }

    private void CopyMachineIdButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(
                MachineIdText.Text);

            MessageBox.Show(
                $"คัดลอกรหัสเครื่องแล้ว\n\n{MachineIdText.Text}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ไม่สามารถคัดลอกรหัสเครื่องได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ImportLicenseButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog =
            new()
            {
                Title = "เลือก ManaChai License",
                Filter =
                    "ManaChai License (*.license)|*.license|All files (*.*)|*.*",
                DefaultExt = ".license",
                CheckFileExists = true,
                Multiselect = false
            };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        LicenseValidationResult result =
            _licenseValidationService
                .InstallLicense(
                    dialog.FileName);

        if (!result.IsValid)
        {
            ShowValidationResult(
                result);

            MessageBox.Show(
                $"ไม่สามารถเปิดใช้งาน License นี้ได้\n\n{result.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return;
        }

        string customer =
            result.Payload?.CustomerName ?? "-";

        MessageBox.Show(
            "เปิดใช้งานสำเร็จ\n\n" +
            $"ลูกค้า: {customer}\n" +
            $"ประเภท: {result.LicenseTypeText}\n" +
            $"หมดอายุ: {result.ExpiryText}",
            AppInfo.StoreName,
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        DialogResult = true;
        Close();
    }

    private void ExitButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
