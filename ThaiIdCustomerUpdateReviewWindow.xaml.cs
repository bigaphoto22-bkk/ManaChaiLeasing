using System.Windows;
using ManaChaiLeasing.Models;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public sealed record ThaiIdCustomerUpdateSelection(
    bool FirstName,
    bool LastName,
    bool Age,
    bool Address)
{
    public int SelectedCount =>
        (FirstName ? 1 : 0) +
        (LastName ? 1 : 0) +
        (Age ? 1 : 0) +
        (Address ? 1 : 0);
}

public partial class ThaiIdCustomerUpdateReviewWindow : Window
{
    private readonly Customer _customer;
    private readonly ThaiIdCardData _cardData;
    private readonly int? _cardAge;

    private readonly bool _firstNameDifferent;
    private readonly bool _lastNameDifferent;
    private readonly bool _ageDifferent;
    private readonly bool _addressDifferent;

    public ThaiIdCustomerUpdateSelection? Selection { get; private set; }

    public ThaiIdCustomerUpdateReviewWindow(
        Customer customer,
        ThaiIdCardData cardData)
    {
        _customer = customer;
        _cardData = cardData;
        _cardAge =
            cardData.CalculateAge(DateTime.Today);

        if (!string.Equals(
                CleanCitizenId(customer.CitizenId),
                CleanCitizenId(cardData.CitizenId),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "เลขบัตรประชาชนของลูกค้าเดิมไม่ตรงกับข้อมูลจากบัตร");
        }

        _firstNameDifferent =
            CanOfferStringUpdate(
                customer.FirstName,
                cardData.ThaiFirstName);

        _lastNameDifferent =
            CanOfferStringUpdate(
                customer.LastName,
                cardData.ThaiLastName);

        _ageDifferent =
            _cardAge.HasValue &&
            customer.Age != _cardAge;

        _addressDifferent =
            CanOfferStringUpdate(
                customer.Address,
                cardData.Address);

        InitializeComponent();

        Title =
            AppInfo.ThaiIdCustomerUpdateReviewWindowTitle;

        ShowComparison();
    }

    private void ShowComparison()
    {
        bool isMock =
            _cardData.Source ==
            ThaiIdCardDataSource.DevelopmentMock;

        MockWarningText.Visibility =
            isMock
                ? Visibility.Visible
                : Visibility.Collapsed;

        CustomerIdentityText.Text =
            $"{_customer.FirstName} {_customer.LastName} • มีข้อมูลอยู่ในระบบแล้ว";

        int differenceCount =
            (_firstNameDifferent ? 1 : 0) +
            (_lastNameDifferent ? 1 : 0) +
            (_ageDifferent ? 1 : 0) +
            (_addressDifferent ? 1 : 0);

        MatchSummaryText.Text =
            differenceCount == 0
                ? "เป็นลูกค้าเก่า • ข้อมูลตรงกับบัตร"
                : $"เป็นลูกค้าเก่า • พบข้อมูลจากบัตรที่ต่างจากระบบ {differenceCount} รายการ";

        UpdateInstructionBorder.Visibility =
            VisibilityFor(
                differenceCount > 0);

        PhoneNoteBorder.Visibility =
            VisibilityFor(
                differenceCount > 0);

        FirstNameChangeRow.Visibility =
            VisibilityFor(
                _firstNameDifferent);

        LastNameChangeRow.Visibility =
            VisibilityFor(
                _lastNameDifferent);

        AgeChangeRow.Visibility =
            VisibilityFor(
                _ageDifferent);

        AddressChangeRow.Visibility =
            VisibilityFor(
                _addressDifferent);

        NoChangesBorder.Visibility =
            differenceCount == 0
                ? Visibility.Visible
                : Visibility.Collapsed;

        ColumnHeaderGrid.Visibility =
            differenceCount == 0
                ? Visibility.Collapsed
                : Visibility.Visible;

        SelectAllButton.IsEnabled =
            differenceCount > 0;

        SelectAllButton.Visibility =
            VisibilityFor(
                differenceCount > 0);

        ApplySelectedButton.Visibility =
            VisibilityFor(
                differenceCount > 0);

        ReviewFooterHintText.Text =
            differenceCount == 0
                ? "กด “ใช้ข้อมูลเดิม” เพื่อดูประวัติการใช้บริการ"
                : "เลือกเฉพาะข้อมูลจากบัตรที่ต้องการนำไปใช้";

        Height =
            differenceCount == 0
                ? 390
                : 650;

        OldFirstNameText.Text =
            Display(
                _customer.FirstName);

        NewFirstNameText.Text =
            Display(
                _cardData.ThaiFirstName);

        OldLastNameText.Text =
            Display(
                _customer.LastName);

        NewLastNameText.Text =
            Display(
                _cardData.ThaiLastName);

        OldAgeText.Text =
            _customer.Age.HasValue
                ? $"{_customer.Age} ปี"
                : "-";

        NewAgeText.Text =
            _cardAge.HasValue
                ? $"{_cardAge} ปี"
                : "-";

        OldAddressText.Text =
            Display(
                _customer.Address);

        NewAddressText.Text =
            Display(
                _cardData.Address);

        UpdateApplyButtonState();
    }

    private void SelectionCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        UpdateApplyButtonState();
    }

    private void SelectAllButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        FirstNameCheckBox.IsChecked =
            _firstNameDifferent;

        LastNameCheckBox.IsChecked =
            _lastNameDifferent;

        AgeCheckBox.IsChecked =
            _ageDifferent;

        AddressCheckBox.IsChecked =
            _addressDifferent;

        UpdateApplyButtonState();
    }

    private void KeepExistingButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Selection =
            new ThaiIdCustomerUpdateSelection(
                false,
                false,
                false,
                false);

        DialogResult = false;
    }

    private void ApplySelectedButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        ThaiIdCustomerUpdateSelection selection =
            BuildSelection();

        if (selection.SelectedCount == 0)
        {
            return;
        }

        Selection =
            selection;

        DialogResult = true;
    }

    private void UpdateApplyButtonState()
    {
        if (ApplySelectedButton is null)
        {
            return;
        }

        int selectedCount =
            BuildSelection()
                .SelectedCount;

        ApplySelectedButton.IsEnabled =
            selectedCount > 0;

        ApplySelectedButton.Content =
            selectedCount > 0
                ? $"นำรายการที่เลือกไปใช้ ({selectedCount})"
                : "นำรายการที่เลือกไปใช้";
    }

    private ThaiIdCustomerUpdateSelection BuildSelection() =>
        new(
            FirstName:
                _firstNameDifferent &&
                FirstNameCheckBox.IsChecked == true,
            LastName:
                _lastNameDifferent &&
                LastNameCheckBox.IsChecked == true,
            Age:
                _ageDifferent &&
                AgeCheckBox.IsChecked == true,
            Address:
                _addressDifferent &&
                AddressCheckBox.IsChecked == true);

    private static Visibility VisibilityFor(
        bool isVisible) =>
        isVisible
            ? Visibility.Visible
            : Visibility.Collapsed;

    private static bool CanOfferStringUpdate(
        string? currentValue,
        string? cardValue)
    {
        if (string.IsNullOrWhiteSpace(
                cardValue))
        {
            // Never propose overwriting a saved value with missing card data.
            return false;
        }

        return !string.Equals(
            NormalizeForComparison(
                currentValue),
            NormalizeForComparison(
                cardValue),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeForComparison(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return string.Empty;
        }

        return string.Join(
            " ",
            value
                .Split(
                    (char[]?)null,
                    StringSplitOptions.RemoveEmptyEntries))
            .Trim();
    }

    private static string CleanCitizenId(
        string? value) =>
        value?.Trim() ?? string.Empty;

    private static string Display(
        string? value) =>
        string.IsNullOrWhiteSpace(
                value)
            ? "-"
            : value.Trim();
}
