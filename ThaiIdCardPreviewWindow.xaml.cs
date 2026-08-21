using System.Windows;
using System.Windows.Media;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class ThaiIdCardPreviewWindow : Window
{
    public ThaiIdCardData CardData { get; }

    public ThaiIdCardPreviewWindow(
        ThaiIdCardData cardData)
    {
        CardData = cardData;

        InitializeComponent();
        ShowCardData();
    }

    private void ShowCardData()
    {
        bool isMock =
            CardData.Source ==
            ThaiIdCardDataSource.DevelopmentMock;

        SourceStatusText.Text = isMock
            ? "DEV MOCK • Parser Test"
            : "อ่านจาก Thai ID Card Reader";

        SourceStatusText.Foreground = isMock
            ? Brushes.DarkOrange
            : Brushes.ForestGreen;

        MockWarningText.Visibility = isMock
            ? Visibility.Visible
            : Visibility.Collapsed;

        CitizenIdText.Text = Display(CardData.CitizenId);
        ThaiNameText.Text = Display(
            JoinName(
                CardData.ThaiPrefix,
                CardData.ThaiFirstName,
                CardData.ThaiLastName));

        EnglishNameText.Text = Display(
            JoinName(
                CardData.EnglishPrefix,
                CardData.EnglishFirstName,
                CardData.EnglishLastName));

        int? age =
            CardData.CalculateAge(DateTime.Today);

        BirthDateText.Text = CardData.BirthDate.HasValue
            ? $"{FormatThaiDate(CardData.BirthDate)}" +
              (age.HasValue
                  ? $" • {age} ปี"
                  : string.Empty)
            : "-";

        GenderText.Text = Display(CardData.Gender);
        IssuerText.Text = Display(CardData.CardIssuer);
        IssueDateText.Text = FormatThaiDate(CardData.IssueDate);
        ExpireDateText.Text = FormatThaiDate(CardData.ExpireDate);
        AddressText.Text = Display(CardData.Address);
    }

    private void ApplyButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static string JoinName(
        params string[] values) =>
        string.Join(
            " ",
            values.Where(value =>
                !string.IsNullOrWhiteSpace(value)));

    private static string Display(
        string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "-"
            : value.Trim();

    private static string FormatThaiDate(
        DateTime? value)
    {
        if (!value.HasValue)
        {
            return "-";
        }

        DateTime date = value.Value;
        int buddhistYear = date.Year + 543;

        return $"{date.Day:00}/{date.Month:00}/{buddhistYear:0000}";
    }
}
