using System.Windows;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class PawnTicketEditWindow : Window
{
    private readonly PawnTicketEditService _editService = new();
    private readonly AutomaticBackupService _automaticBackupService = new();
    private readonly int _pawnTicketId;
    private bool _isLoading = true;
    private bool _isSaving;

    public PawnTicketEditWindow(
        PawnTicketEditData editData)
    {
        InitializeComponent();

        _pawnTicketId = editData.PawnTicketId;
        LoadEditData(editData);
    }

    public PawnTicketEditResult? Result { get; private set; }

    private void LoadEditData(
        PawnTicketEditData data)
    {
        TicketNumberText.Text =
            $"เลขตั๋ว: {data.TicketNumber}";

        LockedTicketSummaryText.Text =
            data.LockedTicketSummary;

        EditableTicketNumberTextBox.Text =
            data.TicketNumber;
        EditablePawnDatePicker.SelectedDate =
            data.PawnDate.Date;

        FirstNameTextBox.Text = data.FirstName;
        LastNameTextBox.Text = data.LastName;
        CitizenIdTextBox.Text = data.CitizenId;
        AgeTextBox.Text = data.Age?.ToString() ?? string.Empty;
        PhoneTextBox.Text = data.Phone;
        AddressTextBox.Text = data.Address;
        AssetCategoryTextBox.Text = data.AssetCategory;
        ProductTypeTextBox.Text = data.ProductType;
        BrandTextBox.Text = data.Brand;
        ModelTextBox.Text = data.Model;
        CapacityOrSizeTextBox.Text = data.CapacityOrSize;
        ColorTextBox.Text = data.Color;
        ImeiOrSerialTextBox.Text = data.ImeiOrSerial;
        AccessoriesTextBox.Text = data.Accessories;
        ConditionTextBox.Text = data.Condition;
        SpecificationTextBox.Text = data.Specification;
        OtherDetailsTextBox.Text = data.OtherDetails;
        ProductSummaryTextBox.Text = data.ProductSummary;
        TicketNoteTextBox.Text = data.Note;

        _isLoading = false;
        UpdateProductSummaryFromFields();

        EditableTicketNumberTextBox.Focus();
        EditableTicketNumberTextBox.SelectAll();
    }

    private void ProductField_TextChanged(
        object sender,
        System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isLoading ||
            ProductSummaryTextBox is null)
        {
            return;
        }

        UpdateProductSummaryFromFields();
    }

    private void UpdateProductSummaryFromFields()
    {
        List<string> parts = [];

        string category =
            AssetCategoryTextBox.Text.Trim();

        bool isMobile =
            category.Contains(
                "โทรศัพท์",
                StringComparison.OrdinalIgnoreCase) ||
            category.Contains(
                "Tablet",
                StringComparison.OrdinalIgnoreCase);

        bool isIt =
            category.Contains(
                "IT",
                StringComparison.OrdinalIgnoreCase);

        bool isElectrical =
            category.Contains(
                "เครื่องใช้ไฟฟ้า",
                StringComparison.OrdinalIgnoreCase);

        if (isMobile)
        {
            AddIfValue(parts, BrandTextBox.Text);
            AddIfValue(parts, ModelTextBox.Text);
            AddIfValue(parts, CapacityOrSizeTextBox.Text);
            AddLabeledValue(parts, "สี", ColorTextBox.Text);
            AddLabeledValue(parts, "IMEI", ImeiOrSerialTextBox.Text);
            AddLabeledValue(parts, "อุปกรณ์", AccessoriesTextBox.Text);
            AddLabeledValue(parts, "สภาพ/ตำหนิ", ConditionTextBox.Text);
        }
        else if (isIt)
        {
            AddIfValue(parts, ProductTypeTextBox.Text);
            AddIfValue(parts, BrandTextBox.Text);
            AddIfValue(parts, ModelTextBox.Text);
            AddIfValue(parts, SpecificationTextBox.Text);
            AddLabeledValue(parts, "Serial", ImeiOrSerialTextBox.Text);
            AddLabeledValue(parts, "อุปกรณ์", AccessoriesTextBox.Text);
            AddLabeledValue(parts, "สภาพ/ตำหนิ", ConditionTextBox.Text);
        }
        else if (isElectrical)
        {
            AddIfValue(parts, ProductTypeTextBox.Text);
            AddIfValue(parts, BrandTextBox.Text);
            AddIfValue(parts, ModelTextBox.Text);
            AddIfValue(parts, CapacityOrSizeTextBox.Text);
            AddLabeledValue(parts, "Serial", ImeiOrSerialTextBox.Text);
            AddLabeledValue(parts, "อุปกรณ์", AccessoriesTextBox.Text);
            AddLabeledValue(parts, "สภาพ/ตำหนิ", ConditionTextBox.Text);
        }
        else
        {
            AddIfValue(parts, ProductTypeTextBox.Text);
            AddIfValue(parts, BrandTextBox.Text);
            AddIfValue(parts, ModelTextBox.Text);
            AddIfValue(parts, OtherDetailsTextBox.Text);
            AddLabeledValue(parts, "Serial", ImeiOrSerialTextBox.Text);
            AddLabeledValue(parts, "อุปกรณ์", AccessoriesTextBox.Text);
            AddLabeledValue(parts, "สภาพ/ตำหนิ", ConditionTextBox.Text);
        }

        ProductSummaryTextBox.Text =
            string.Join(" / ", parts);
    }

    private static void AddIfValue(
        ICollection<string> parts,
        string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            parts.Add(cleaned);
        }
    }

    private static void AddLabeledValue(
        ICollection<string> parts,
        string label,
        string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            parts.Add($"{label} {cleaned}");
        }
    }

    private void SaveEditButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        if (_isSaving ||
            !TryBuildRequest(
                out PawnTicketEditRequest request))
        {
            return;
        }

        MessageBoxResult confirmation =
            MessageBox.Show(
                "ยืนยันบันทึกการแก้ไขข้อมูลตั๋วนี้หรือไม่\n\n" +
                "เลขตั๋วหรือวันที่จำนำที่แก้ไขจะมีผลกับหน้าค้นหาและประวัติตั๋ว\n" +
                "ระบบจะเก็บเหตุผลและค่าก่อน–หลังไว้ในประวัติการแก้ไข",
                AppInfo.StoreName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        _isSaving = true;
        SaveEditButton.IsEnabled = false;
        SaveEditButton.Content = "กำลังบันทึก...";

        try
        {
            Result =
                _editService.Save(request);

            AutomaticBackupExecutionResult backupResult =
                _automaticBackupService.RunAutomaticBackup();

            if (backupResult.IsFailed)
            {
                MessageBox.Show(
                    "บันทึกการแก้ไขเรียบร้อยแล้ว แต่ Auto Backup ไม่สำเร็จ\n\n" +
                    $"{backupResult.ErrorMessage}\n\n" +
                    "กรุณาตรวจ Drive สำรองข้อมูลที่หน้า ตั้งค่า",
                    "Auto Backup ไม่สำเร็จ",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            MessageBox.Show(
                $"บันทึกการแก้ไขเรียบร้อยแล้ว\n\n" +
                $"เปลี่ยนแปลง {Result.ChangedFieldCount:N0} ช่อง\n" +
                $"เวลา {Result.EditedAt:dd/MM/yyyy HH:mm}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            DialogResult = true;
        }
        catch (Exception ex)
        {
            AppLog.Error(
                "Could not save controlled pawn ticket edit.",
                ex);

            MessageBox.Show(
                $"ไม่สามารถบันทึกการแก้ไขได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isSaving = false;
            SaveEditButton.IsEnabled = true;
            SaveEditButton.Content = "บันทึกการแก้ไข";
        }
    }

    private bool TryBuildRequest(
        out PawnTicketEditRequest request)
    {
        request = new PawnTicketEditRequest();

        string ticketNumber =
            EditableTicketNumberTextBox.Text.Trim();
        string firstName = FirstNameTextBox.Text.Trim();
        string lastName = LastNameTextBox.Text.Trim();
        string citizenId = CitizenIdTextBox.Text.Trim();
        string ageText = AgeTextBox.Text.Trim();
        string assetCategory = AssetCategoryTextBox.Text.Trim();
        string productSummary = ProductSummaryTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(ticketNumber))
        {
            ShowValidation(
                "กรุณากรอกเลขตั๋ว");

            EditableTicketNumberTextBox.Focus();
            return false;
        }

        if (!EditablePawnDatePicker.SelectedDate.HasValue)
        {
            ShowValidation(
                "กรุณาเลือกวันที่จำนำ");

            EditablePawnDatePicker.Focus();
            return false;
        }

        DateTime pawnDate =
            EditablePawnDatePicker.SelectedDate.Value.Date;

        if (pawnDate > DateTime.Today)
        {
            ShowValidation(
                "วันที่จำนำต้องไม่เกินวันนี้");

            EditablePawnDatePicker.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName))
        {
            ShowValidation(
                "กรุณากรอกชื่อและนามสกุลลูกค้า");

            FirstNameTextBox.Focus();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(citizenId) &&
            (citizenId.Length != 13 ||
             !citizenId.All(char.IsDigit)))
        {
            ShowValidation(
                "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก หรือเว้นว่างไว้");

            CitizenIdTextBox.Focus();
            return false;
        }

        int? age = null;

        if (!string.IsNullOrWhiteSpace(ageText))
        {
            if (!int.TryParse(
                    ageText,
                    out int parsedAge) ||
                parsedAge < 1 ||
                parsedAge > 120)
            {
                ShowValidation(
                    "อายุต้องเป็นตัวเลขระหว่าง 1 - 120 ปี หรือเว้นว่างไว้");

                AgeTextBox.Focus();
                return false;
            }

            age = parsedAge;
        }

        if (string.IsNullOrWhiteSpace(assetCategory))
        {
            ShowValidation(
                "กรุณากรอกประเภทหลักของสินค้า");

            AssetCategoryTextBox.Focus();
            return false;
        }

        if (string.IsNullOrWhiteSpace(productSummary))
        {
            ShowValidation(
                "กรุณากรอกรายละเอียดสินค้าสรุป");

            ProductSummaryTextBox.Focus();
            return false;
        }

        request = new PawnTicketEditRequest
        {
            PawnTicketId = _pawnTicketId,
            TicketNumber = ticketNumber,
            PawnDate = pawnDate,
            FirstName = firstName,
            LastName = lastName,
            CitizenId = citizenId,
            Age = age,
            Phone = PhoneTextBox.Text,
            Address = AddressTextBox.Text,
            AssetCategory = assetCategory,
            ProductType = ProductTypeTextBox.Text,
            Brand = BrandTextBox.Text,
            Model = ModelTextBox.Text,
            CapacityOrSize = CapacityOrSizeTextBox.Text,
            Color = ColorTextBox.Text,
            ImeiOrSerial = ImeiOrSerialTextBox.Text,
            Accessories = AccessoriesTextBox.Text,
            Condition = ConditionTextBox.Text,
            Specification = SpecificationTextBox.Text,
            OtherDetails = OtherDetailsTextBox.Text,
            ProductSummary = productSummary,
            Note = TicketNoteTextBox.Text,
            Reason = EditReasonTextBox.Text
        };

        return true;
    }

    private static void ShowValidation(
        string message)
    {
        MessageBox.Show(
            message,
            AppInfo.StoreName,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void CancelButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
