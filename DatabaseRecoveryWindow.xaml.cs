using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using ManaChaiLeasing.Data;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class DatabaseRecoveryWindow : Window
{
    private readonly DatabaseHealthResult _healthResult;

    private readonly DatabaseBackupService _databaseBackupService = new();

    private readonly AutomaticBackupService _automaticBackupService = new();

    public bool RecoveryCompleted { get; private set; }

    public DatabaseRecoveryWindow(
        DatabaseHealthResult healthResult)
    {
        _healthResult =
            healthResult;

        InitializeComponent();

        LoadDetails();
    }

    private void LoadDetails()
    {
        IssueMessageText.Text =
            _healthResult.UserMessage;

        TechnicalDetailsText.Text =
            _healthResult.TechnicalMessage;

        DatabasePathText.Text =
            DatabasePaths.DatabaseFile;

        AutomaticBackupSettings backupSettings =
            _automaticBackupService.GetSettings();

        AutomaticBackupFolderText.Text =
            string.IsNullOrWhiteSpace(
                backupSettings.BackupFolder)
                ? "Auto Backup: ยังไม่ได้กำหนดโฟลเดอร์"
                : $"Auto Backup: {backupSettings.BackupFolder}";
    }

    private void RestoreBackupButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        OpenFileDialog dialog =
            new()
            {
                Title =
                    "เลือกไฟล์ Backup ที่ต้องการใช้กู้คืนฐานข้อมูล",
                Filter =
                    "ManaChaiLeasing Backup (*.db)|*.db|All files (*.*)|*.*",
                DefaultExt = ".db",
                CheckFileExists = true,
                Multiselect = false
            };

        AutomaticBackupSettings settings =
            _automaticBackupService.GetSettings();

        if (!string.IsNullOrWhiteSpace(
                settings.BackupFolder) &&
            Directory.Exists(
                settings.BackupFolder))
        {
            dialog.InitialDirectory =
                settings.BackupFolder;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            DatabaseBackupInfo info =
                _databaseBackupService.InspectBackup(
                    dialog.FileName);

            MessageBoxResult confirm =
                MessageBox.Show(
                    "ตรวจพบ Backup ที่ใช้งานได้\n\n" +
                    $"ชื่อร้าน: {info.StoreName}\n" +
                    $"ลูกค้า: {info.CustomerCount:N0} ราย\n" +
                    $"ตั๋วจำนำ: {info.PawnTicketCount:N0} ใบ\n" +
                    $"ประวัติรายการ: {info.TransactionCount:N0} รายการ\n" +
                    $"วันที่ไฟล์: {info.FileModifiedAt:dd/MM/yyyy HH:mm}\n\n" +
                    "ระบบจะเก็บสำเนาฐานข้อมูลที่มีปัญหาไว้ก่อนกู้คืน\n\n" +
                    "ยืนยันใช้ Backup นี้หรือไม่?",
                    "ยืนยันกู้คืนฐานข้อมูล",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

            if (confirm !=
                MessageBoxResult.Yes)
            {
                return;
            }

            AppLog.Warning(
                "Database recovery restore started.");

            DatabaseRecoveryRestoreResult result =
                _databaseBackupService
                    .RestoreBackupForRecovery(
                        dialog.FileName);

            AppLog.Info(
                "Database recovery restore completed successfully.");

            RecoveryCompleted = true;

            MessageBox.Show(
                "กู้คืนฐานข้อมูลเรียบร้อย\n\n" +
                "สำเนาฐานข้อมูลเดิมที่มีปัญหาถูกเก็บไว้ที่:\n" +
                $"{result.PreservedDatabaseFolder}\n\n" +
                "โปรแกรมจะปิดในตอนนี้ กรุณาเปิดใหม่อีกครั้ง",
                "กู้คืนฐานข้อมูลสำเร็จ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Database recovery restore failed.",
                ex);

            MessageBox.Show(
                "ไม่สามารถกู้คืนฐานข้อมูลได้\n\n" +
                $"{ex.Message}\n\n" +
                "โปรแกรมจะยังไม่อนุญาตให้ทำรายการ กรุณาลอง Backup ไฟล์อื่นหรือตรวจสอบ Technical Log",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenAutoBackupFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            AutomaticBackupSettings settings =
                _automaticBackupService.GetSettings();

            if (string.IsNullOrWhiteSpace(
                    settings.BackupFolder))
            {
                MessageBox.Show(
                    "ยังไม่ได้กำหนดโฟลเดอร์ Auto Backup",
                    AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (!Directory.Exists(
                    settings.BackupFolder))
            {
                MessageBox.Show(
                    "ไม่พบโฟลเดอร์ Auto Backup\n\n" +
                    $"{settings.BackupFolder}\n\n" +
                    "กรุณาตรวจว่า External Drive หรือ Flash Drive เชื่อมต่ออยู่",
                    AppInfo.StoreName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        settings.BackupFolder,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not open automatic backup folder from recovery window.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถเปิดโฟลเดอร์ Auto Backup ได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenLogFolderButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(
                AppLog.LogFolder);

            Process.Start(
                new ProcessStartInfo
                {
                    FileName =
                        AppLog.LogFolder,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"ไม่สามารถเปิดโฟลเดอร์ Log ได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ExitButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
