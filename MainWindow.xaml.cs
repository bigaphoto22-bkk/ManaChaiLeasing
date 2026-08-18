using System.Windows;
using System.Windows.Controls;

namespace ManaChaiLeasing;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
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
