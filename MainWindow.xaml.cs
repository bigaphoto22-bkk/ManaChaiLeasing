using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ManaChaiLeasing.Data;

namespace ManaChaiLeasing;

public partial class MainWindow : Window
{
    private bool _isInitializing = true;

    public MainWindow()
    {
        InitializeComponent();

        InitializeDatabase();

        PawnDatePicker.SelectedDate = DateTime.Today;

        _isInitializing = false;
        UpdateProductForm();
        UpdateAssetPreview();
    }

    private void InitializeDatabase()
    {
        try
        {
            DatabaseInitializer.Initialize();

            DatabaseStatusText.Text = "Offline • SQLite Ready";
            DatabaseStatusText.Foreground = Brushes.ForestGreen;
            DatabaseStatusText.ToolTip = DatabasePaths.DatabaseFile;
        }
        catch (Exception ex)
        {
            DatabaseStatusText.Text = "Database Error";
            DatabaseStatusText.Foreground = Brushes.Firebrick;

            MessageBox.Show(
                $"ไม่สามารถเตรียมฐานข้อมูล SQLite ได้\n\n{ex.Message}",
                "มานะชัย ลิสซิ่ง",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void HomeButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            HomeContent,
            HomeButton,
            "หน้าหลัก",
            "ระบบบันทึกข้อมูลลูกค้าและรายการรับจำนำ");
    }

    private void NewPawnButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            NewPawnContent,
            NewPawnButton,
            "รับจำนำใหม่",
            "บันทึกข้อมูลลูกค้า สินค้า หมายเลขตั๋ว และยอดเงิน");
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            SearchContent,
            SearchButton,
            "ค้นหารายการ",
            "ค้นหาข้อมูลและประวัติรายการย้อนหลัง");
    }

    private void TodayButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            TodayContent,
            TodayButton,
            "รายการวันนี้",
            "สรุปรายการรับจำนำ ต่อดอก ไถ่ถอน และยอดประจำวัน");
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(
            SettingsContent,
            SettingsButton,
            "ตั้งค่า",
            "ตั้งค่าข้อมูลร้านและเงื่อนไขการใช้งานระบบ");
    }

    private void AssetCategoryComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateProductForm();
        UpdateAssetPreview();
    }

    private void SmartField_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateAssetPreview();
    }

    private void SmartField_KeyUp(
        object sender,
        KeyEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateAssetPreview();
    }

    private void SmartField_TextChanged(
        object sender,
        TextChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        UpdateAssetPreview();
    }

    private void UpdateProductForm()
    {
        if (MobileProductPanel is null ||
            ItProductPanel is null ||
            ElectricalProductPanel is null ||
            OtherProductPanel is null)
        {
            return;
        }

        MobileProductPanel.Visibility = Visibility.Collapsed;
        ItProductPanel.Visibility = Visibility.Collapsed;
        ElectricalProductPanel.Visibility = Visibility.Collapsed;
        OtherProductPanel.Visibility = Visibility.Collapsed;

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                MobileProductPanel.Visibility = Visibility.Visible;
                break;

            case 1:
                ItProductPanel.Visibility = Visibility.Visible;
                break;

            case 2:
                ElectricalProductPanel.Visibility = Visibility.Visible;
                break;

            default:
                OtherProductPanel.Visibility = Visibility.Visible;
                break;
        }
    }

    private void UpdateAssetPreview()
    {
        if (_isInitializing || AssetPreviewText is null)
        {
            return;
        }

        List<string> parts = new();

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                AddIfValue(parts, GetComboText(MobileBrandComboBox));
                AddIfValue(parts, GetComboText(MobileModelComboBox));
                AddIfValue(parts, GetComboText(MobileCapacityComboBox));

                string mobileColor = GetComboText(MobileColorComboBox);
                if (!string.IsNullOrWhiteSpace(mobileColor))
                {
                    parts.Add($"สี {mobileColor}");
                }

                AddLabeledValue(parts, "IMEI", MobileImeiTextBox.Text);
                AddLabeledValue(parts, "อุปกรณ์", MobileAccessoriesTextBox.Text);
                AddLabeledValue(parts, "สภาพ/ตำหนิ", MobileConditionTextBox.Text);
                break;

            case 1:
                AddIfValue(parts, GetComboText(ItTypeComboBox));
                AddIfValue(parts, GetComboText(ItBrandComboBox));
                AddIfValue(parts, GetComboText(ItModelComboBox));
                AddIfValue(parts, ItSpecificationTextBox.Text);
                AddLabeledValue(parts, "Serial", ItSerialTextBox.Text);
                AddLabeledValue(parts, "อุปกรณ์", ItAccessoriesTextBox.Text);
                AddLabeledValue(parts, "สภาพ/ตำหนิ", ItConditionTextBox.Text);
                break;

            case 2:
                AddIfValue(parts, GetComboText(ElectricalTypeComboBox));
                AddIfValue(parts, GetComboText(ElectricalBrandComboBox));
                AddIfValue(parts, GetComboText(ElectricalModelComboBox));
                AddIfValue(parts, ElectricalSizeTextBox.Text);
                AddLabeledValue(parts, "Serial", ElectricalSerialTextBox.Text);
                AddLabeledValue(parts, "อุปกรณ์", ElectricalAccessoriesTextBox.Text);
                AddLabeledValue(parts, "สภาพ/ตำหนิ", ElectricalConditionTextBox.Text);
                break;

            default:
                AddIfValue(parts, OtherTypeTextBox.Text);
                AddIfValue(parts, OtherBrandTextBox.Text);
                AddIfValue(parts, OtherModelTextBox.Text);
                AddIfValue(parts, OtherDetailsTextBox.Text);
                AddLabeledValue(parts, "Serial", OtherSerialTextBox.Text);
                AddLabeledValue(parts, "อุปกรณ์", OtherAccessoriesTextBox.Text);
                AddLabeledValue(parts, "สภาพ/ตำหนิ", OtherConditionTextBox.Text);
                break;
        }

        AssetPreviewText.Text = parts.Count == 0
            ? "กรอกข้อมูลสินค้า แล้วระบบจะสร้างรายละเอียดสรุปให้อัตโนมัติ"
            : string.Join(" / ", parts);
    }

    private static string GetComboText(ComboBox comboBox)
    {
        if (!string.IsNullOrWhiteSpace(comboBox.Text))
        {
            return comboBox.Text.Trim();
        }

        if (comboBox.SelectedItem is ComboBoxItem selectedItem &&
            selectedItem.Content is not null)
        {
            return selectedItem.Content.ToString()?.Trim() ?? string.Empty;
        }

        return string.Empty;
    }

    private static void AddIfValue(
        List<string> parts,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add(value.Trim());
        }
    }

    private static void AddLabeledValue(
        List<string> parts,
        string label,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value.Trim()}");
        }
    }

    private void ClearNewPawnForm_Click(object sender, RoutedEventArgs e)
    {
        _isInitializing = true;

        TicketNumberTextBox.Clear();
        PawnDatePicker.SelectedDate = DateTime.Today;

        FirstNameTextBox.Clear();
        LastNameTextBox.Clear();
        CitizenIdTextBox.Clear();
        AgeTextBox.Clear();
        PhoneTextBox.Clear();
        AddressTextBox.Clear();

        AssetCategoryComboBox.SelectedIndex = 0;
        PawnAmountTextBox.Clear();

        MobileBrandComboBox.SelectedIndex = -1;
        MobileBrandComboBox.Text = string.Empty;
        MobileModelComboBox.SelectedIndex = -1;
        MobileModelComboBox.Text = string.Empty;
        MobileCapacityComboBox.SelectedIndex = -1;
        MobileCapacityComboBox.Text = string.Empty;
        MobileColorComboBox.SelectedIndex = -1;
        MobileColorComboBox.Text = string.Empty;
        MobileImeiTextBox.Clear();
        MobileAccessoriesTextBox.Clear();
        MobileConditionTextBox.Clear();

        ItTypeComboBox.SelectedIndex = -1;
        ItTypeComboBox.Text = string.Empty;
        ItBrandComboBox.SelectedIndex = -1;
        ItBrandComboBox.Text = string.Empty;
        ItModelComboBox.SelectedIndex = -1;
        ItModelComboBox.Text = string.Empty;
        ItSpecificationTextBox.Clear();
        ItSerialTextBox.Clear();
        ItAccessoriesTextBox.Clear();
        ItConditionTextBox.Clear();

        ElectricalTypeComboBox.SelectedIndex = -1;
        ElectricalTypeComboBox.Text = string.Empty;
        ElectricalBrandComboBox.SelectedIndex = -1;
        ElectricalBrandComboBox.Text = string.Empty;
        ElectricalModelComboBox.SelectedIndex = -1;
        ElectricalModelComboBox.Text = string.Empty;
        ElectricalSizeTextBox.Clear();
        ElectricalSerialTextBox.Clear();
        ElectricalAccessoriesTextBox.Clear();
        ElectricalConditionTextBox.Clear();

        OtherTypeTextBox.Clear();
        OtherBrandTextBox.Clear();
        OtherModelTextBox.Clear();
        OtherDetailsTextBox.Clear();
        OtherSerialTextBox.Clear();
        OtherAccessoriesTextBox.Clear();
        OtherConditionTextBox.Clear();

        PawnNoteTextBox.Clear();

        _isInitializing = false;

        UpdateProductForm();
        UpdateAssetPreview();

        TicketNumberTextBox.Focus();
    }

    private void ShowPage(
        UIElement pageToShow,
        Button activeButton,
        string pageTitle,
        string pageSubtitle)
    {
        HomeContent.Visibility = Visibility.Collapsed;
        NewPawnContent.Visibility = Visibility.Collapsed;
        SearchContent.Visibility = Visibility.Collapsed;
        TodayContent.Visibility = Visibility.Collapsed;
        SettingsContent.Visibility = Visibility.Collapsed;

        pageToShow.Visibility = Visibility.Visible;

        HomeButton.Style = (Style)FindResource("SidebarButtonStyle");
        NewPawnButton.Style = (Style)FindResource("SidebarButtonStyle");
        SearchButton.Style = (Style)FindResource("SidebarButtonStyle");
        TodayButton.Style = (Style)FindResource("SidebarButtonStyle");
        SettingsButton.Style = (Style)FindResource("SidebarButtonStyle");

        activeButton.Style = (Style)FindResource("SidebarActiveButtonStyle");

        PageTitleText.Text = pageTitle;
        PageSubtitleText.Text = pageSubtitle;
    }
}
