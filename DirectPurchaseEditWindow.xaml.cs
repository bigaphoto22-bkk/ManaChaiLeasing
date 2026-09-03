using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using ManaChaiLeasing.Models;
using ManaChaiLeasing.Services;

namespace ManaChaiLeasing;

public partial class DirectPurchaseEditWindow : Window
{
    private readonly DirectPurchaseService _service = new();
    private readonly CustomerService _customerService = new();
    private readonly PawnTicketService _pawnTicketService = new();
    private readonly ThaiIdCardReaderService _thaiIdReaderService = new();
    private readonly AutomaticBackupService _backupService = new();
    private readonly int? _purchaseId;
    private int? _selectedSellerCustomerId;
    private DirectPurchaseStatus? _currentStatus;
    private bool _isInitializing = true;
    private bool _isSaving;

    public DirectPurchaseEditWindow(int? purchaseId = null)
    {
        InitializeComponent();
        _purchaseId = purchaseId;
        PurchaseDatePicker.SelectedDate = DateTime.Today;
        PurchaseDatePicker.DisplayDateEnd = DateTime.Today;
        AssetCategoryComboBox.SelectedIndex = 0;
        LoadSmartLookupValues();

        if (purchaseId.HasValue)
        {
            LoadExisting(purchaseId.Value);
        }

        _isInitializing = false;
        UpdateProductForm();
        UpdateAssetPreview();
    }

    private void LoadExisting(int id)
    {
        try
        {
            DirectPurchaseData data = _service.Get(id);
            _currentStatus = data.Status;
            HeadingText.Text = "รายละเอียดรายการรับซื้อ";
            StatusText.Text = DirectPurchaseService.StatusText(data.Status);
            _selectedSellerCustomerId = data.SellerCustomerId;
            SelectedSellerText.Text = $"ผู้ขายเดิมในระบบ • รหัสลูกค้า {data.SellerCustomerId:N0}";

            DocumentNumberTextBox.Text = data.DocumentNumber;
            PurchaseDatePicker.SelectedDate = data.PurchaseDate;
            PurchasePriceTextBox.Text = data.PurchasePrice.ToString("N2");
            FirstNameTextBox.Text = data.FirstName; LastNameTextBox.Text = data.LastName;
            CitizenIdTextBox.Text = data.CitizenId; AgeTextBox.Text = data.Age?.ToString() ?? string.Empty;
            PhoneTextBox.Text = data.Phone; AddressTextBox.Text = data.Address;
            LoadProductData(data);
            NoteTextBox.Text = data.Note;
            SetComboText(PaymentMethodComboBox, data.PaymentMethod);

            if (data.Status == DirectPurchaseStatus.InStock)
            {
                SaveButton.Content = "บันทึกการแก้ไข";
                EditReasonBorder.Visibility = Visibility.Visible;
                SellButton.Visibility = Visibility.Visible;
            }
            else
            {
                EditableFormPanel.IsEnabled = false;
                SaveButton.Visibility = Visibility.Collapsed;
                StatusBorder.Background = data.Status == DirectPurchaseStatus.Cancelled
                    ? System.Windows.Media.Brushes.MistyRose
                    : System.Windows.Media.Brushes.AliceBlue;

                if (data.Status == DirectPurchaseStatus.Cancelled)
                {
                    CancellationBorder.Visibility = Visibility.Visible;
                    CancellationReasonText.Text = data.CancellationReason;
                }
                else if (data.Status == DirectPurchaseStatus.Sold && data.SalePrice.HasValue)
                {
                    SellButton.Visibility = Visibility.Visible;
                    SellButton.Content = "แก้ไขข้อมูลการขาย";
                    SaleSummaryBorder.Visibility = Visibility.Visible;
                    SaleDateText.Text = data.SaleDate?.ToString("dd/MM/yyyy") ?? "-";
                    SalePurchasePriceText.Text = $"{data.PurchasePrice:N2} บาท";
                    SalePriceText.Text = $"{data.SalePrice.Value:N2} บาท";
                    decimal profit = data.SaleProfit ?? 0m;
                    SaleProfitText.Text = profit >= 0m
                        ? $"กำไร {profit:N2} บาท"
                        : $"ขาดทุน {Math.Abs(profit):N2} บาท";
                    SaleProfitText.Foreground = profit >= 0m
                        ? System.Windows.Media.Brushes.ForestGreen
                        : System.Windows.Media.Brushes.Firebrick;
                    SalePaymentMethodText.Text = string.IsNullOrWhiteSpace(data.SalePaymentMethod)
                        ? "-"
                        : data.SalePaymentMethod;
                    SaleNoteText.Text = string.IsNullOrWhiteSpace(data.SaleNote)
                        ? "-"
                        : data.SaleNote;
                }
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not load direct purchase detail.", ex);
            MessageBox.Show($"ไม่สามารถเปิดรายละเอียดรายการรับซื้อได้\n\n{ex.Message}", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Error);
            Loaded += (_, _) => Close();
        }
    }

    private void LookupCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        CustomerLookupWindow window = new() { Owner = this };
        if (window.ShowDialog() == true && window.SelectedCustomer is Customer customer)
        {
            ApplyCustomer(customer);
        }
    }

    private void ApplyCustomer(Customer customer)
    {
        _selectedSellerCustomerId = customer.Id;
        FirstNameTextBox.Text = customer.FirstName; LastNameTextBox.Text = customer.LastName;
        CitizenIdTextBox.Text = customer.CitizenId ?? string.Empty;
        AgeTextBox.Text = customer.Age?.ToString() ?? string.Empty;
        PhoneTextBox.Text = customer.Phone ?? string.Empty; AddressTextBox.Text = customer.Address ?? string.Empty;
        SelectedSellerText.Text = $"เลือกผู้ขายเดิมแล้ว • รหัสลูกค้า {customer.Id:N0}";
    }

    private void ReadThaiIdButton_Click(object sender, RoutedEventArgs e)
    {
        Mouse.OverrideCursor = Cursors.Wait;
        ThaiIdCardReadResult result;
        try
        {
            result = _thaiIdReaderService.ReadCard();
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        if (!result.Success || result.Data is null)
        {
            MessageBox.Show($"{result.UserMessage}\n\nสามารถกรอกข้อมูลผู้ขายด้วยตนเองได้ตามปกติ", "อ่านบัตรประชาชนไม่สำเร็จ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ThaiIdCardData data = result.Data;
        if (MessageBox.Show($"พบข้อมูล {data.ThaiFirstName} {data.ThaiLastName}\nต้องการนำข้อมูลนี้มาใช้เป็นผู้ขายหรือไม่?", "ตรวจสอบข้อมูลบัตรประชาชน", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        Customer? existing = _customerService.FindByCitizenId(data.CitizenId);
        _selectedSellerCustomerId = existing?.Id;
        FirstNameTextBox.Text = data.ThaiFirstName; LastNameTextBox.Text = data.ThaiLastName;
        CitizenIdTextBox.Text = data.CitizenId; AgeTextBox.Text = data.CalculateAge(DateTime.Today)?.ToString() ?? string.Empty;
        AddressTextBox.Text = data.Address;
        if (existing is not null && string.IsNullOrWhiteSpace(PhoneTextBox.Text)) PhoneTextBox.Text = existing.Phone ?? string.Empty;
        SelectedSellerText.Text = existing is null
            ? "อ่านบัตรสำเร็จ • จะสร้างข้อมูลผู้ขายรายใหม่"
            : $"อ่านบัตรสำเร็จ • เชื่อมกับลูกค้าเดิมรหัส {existing.Id:N0}";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isSaving) return;
        _isSaving = true;
        SaveButton.IsEnabled = false;

        try
        {
            if (!PurchaseDatePicker.SelectedDate.HasValue)
                throw new InvalidOperationException("กรุณาเลือกวันที่รับซื้อ");
            if (!TryParseAmount(PurchasePriceTextBox.Text, out decimal price))
                throw new InvalidOperationException("กรุณากรอกราคารับซื้อให้ถูกต้อง");
            int? age = ParseAge(AgeTextBox.Text);
            string summary = BuildProductSummary();

            DirectPurchaseSaveRequest request = new()
            {
                Id = _purchaseId,
                SelectedSellerCustomerId = _selectedSellerCustomerId,
                DocumentNumber = DocumentNumberTextBox.Text,
                PurchaseDate = PurchaseDatePicker.SelectedDate.Value,
                PurchasePrice = price,
                FirstName = FirstNameTextBox.Text,
                LastName = LastNameTextBox.Text,
                CitizenId = CitizenIdTextBox.Text,
                Age = age,
                Phone = PhoneTextBox.Text,
                Address = AddressTextBox.Text,
                AssetCategory = GetAssetCategoryName(),
                ProductType = GetCurrentProductType(),
                Brand = GetCurrentBrand(),
                Model = GetCurrentModel(),
                CapacityOrSize = GetCurrentCapacityOrSize(),
                Color = GetCurrentColor(),
                ImeiOrSerial = GetCurrentSerial(),
                Accessories = GetCurrentAccessories(),
                Condition = GetCurrentCondition(),
                Specification = GetCurrentSpecification(),
                OtherDetails = GetCurrentOtherDetails(),
                ProductSummary = summary,
                PaymentMethod = GetComboText(PaymentMethodComboBox),
                Note = NoteTextBox.Text,
                EditReason = EditReasonTextBox.Text,
                SmartLookupValues = BuildSmartLookupEntries()
            };

            int id = _service.Save(request);
            AutomaticBackupExecutionResult backup = _backupService.RunAutomaticBackup();
            string backupWarning = backup.IsFailed
                ? $"\n\nหมายเหตุ: Auto Backup ไม่สำเร็จ\n{backup.ErrorMessage}"
                : string.Empty;

            MessageBox.Show(
                (_purchaseId.HasValue ? "บันทึกการแก้ไขเรียบร้อยแล้ว" : "บันทึกรายการรับซื้อเรียบร้อยแล้ว") +
                $"\nรหัสรายการภายใน: {id:N0}" + backupWarning,
                AppInfo.StoreName, MessageBoxButton.OK,
                backup.IsFailed ? MessageBoxImage.Warning : MessageBoxImage.Information);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not save direct purchase.", ex);
            MessageBox.Show($"ไม่สามารถบันทึกรายการรับซื้อได้\n\n{ex.Message}", AppInfo.StoreName, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (DialogResult != true)
            {
                _isSaving = false;
                SaveButton.IsEnabled = true;
            }
        }
    }

    private void SellButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_purchaseId.HasValue)
        {
            return;
        }

        try
        {
            DirectPurchaseSalePreview preview = _currentStatus == DirectPurchaseStatus.Sold
                ? _service.GetSaleEditPreview(_purchaseId.Value)
                : _service.GetSalePreview(_purchaseId.Value);
            DirectPurchaseSaleWindow window = new(preview) { Owner = this };
            if (window.ShowDialog() == true)
            {
                DialogResult = true;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Could not open direct purchase sale or correction from detail.", ex);
            MessageBox.Show(
                $"ไม่สามารถเปิดข้อมูลการขายได้\n\n{ex.Message}",
                AppInfo.StoreName,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private string BuildProductSummary()
    {
        List<string> parts = [];

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                Add(parts, GetComboText(MobileBrandComboBox));
                Add(parts, GetComboText(MobileModelComboBox));
                Add(parts, GetComboText(MobileCapacityComboBox));
                AddLabeled(parts, "สี", GetComboText(MobileColorComboBox));
                AddLabeled(parts, "IMEI", MobileImeiTextBox.Text);
                AddLabeled(parts, "อุปกรณ์", MobileAccessoriesTextBox.Text);
                AddLabeled(parts, "สภาพ/ตำหนิ", MobileConditionTextBox.Text);
                break;

            case 1:
                Add(parts, GetComboText(ItTypeComboBox));
                Add(parts, GetComboText(ItBrandComboBox));
                Add(parts, GetComboText(ItModelComboBox));
                Add(parts, ItSpecificationTextBox.Text);
                AddLabeled(parts, "Serial", ItSerialTextBox.Text);
                AddLabeled(parts, "อุปกรณ์", ItAccessoriesTextBox.Text);
                AddLabeled(parts, "สภาพ/ตำหนิ", ItConditionTextBox.Text);
                break;

            case 2:
                Add(parts, GetComboText(ElectricalTypeComboBox));
                Add(parts, GetComboText(ElectricalBrandComboBox));
                Add(parts, GetComboText(ElectricalModelComboBox));
                Add(parts, ElectricalSizeTextBox.Text);
                AddLabeled(parts, "Serial", ElectricalSerialTextBox.Text);
                AddLabeled(parts, "อุปกรณ์", ElectricalAccessoriesTextBox.Text);
                AddLabeled(parts, "สภาพ/ตำหนิ", ElectricalConditionTextBox.Text);
                break;

            default:
                Add(parts, OtherTypeTextBox.Text);
                Add(parts, OtherBrandTextBox.Text);
                Add(parts, OtherModelTextBox.Text);
                Add(parts, OtherDetailsTextBox.Text);
                AddLabeled(parts, "Serial", OtherSerialTextBox.Text);
                AddLabeled(parts, "อุปกรณ์", OtherAccessoriesTextBox.Text);
                AddLabeled(parts, "สภาพ/ตำหนิ", OtherConditionTextBox.Text);
                break;
        }

        return string.Join(" / ", parts);
    }

    private static void Add(List<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) parts.Add(value.Trim());
    }

    private static void AddLabeled(
        List<string> parts,
        string label,
        string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{label}: {value.Trim()}");
        }
    }

    private void LoadProductData(DirectPurchaseData data)
    {
        AssetCategoryComboBox.SelectedIndex =
            data.AssetCategory.Contains("โทรศัพท์", StringComparison.OrdinalIgnoreCase) ||
            data.AssetCategory.Contains("Tablet", StringComparison.OrdinalIgnoreCase)
                ? 0
                : data.AssetCategory.Contains("IT", StringComparison.OrdinalIgnoreCase)
                    ? 1
                    : data.AssetCategory.Contains("เครื่องใช้ไฟฟ้า", StringComparison.OrdinalIgnoreCase)
                        ? 2
                        : 3;

        UpdateProductForm();

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                SetComboText(MobileBrandComboBox, data.Brand);
                SetComboText(MobileModelComboBox, data.Model);
                SetComboText(MobileCapacityComboBox, data.CapacityOrSize);
                SetComboText(MobileColorComboBox, data.Color);
                MobileImeiTextBox.Text = data.ImeiOrSerial;
                MobileAccessoriesTextBox.Text = data.Accessories;
                MobileConditionTextBox.Text = data.Condition;
                break;

            case 1:
                SetComboText(ItTypeComboBox, data.ProductType);
                SetComboText(ItBrandComboBox, data.Brand);
                SetComboText(ItModelComboBox, data.Model);
                ItSpecificationTextBox.Text = data.Specification;
                ItSerialTextBox.Text = data.ImeiOrSerial;
                ItAccessoriesTextBox.Text = data.Accessories;
                ItConditionTextBox.Text = data.Condition;
                break;

            case 2:
                SetComboText(ElectricalTypeComboBox, data.ProductType);
                SetComboText(ElectricalBrandComboBox, data.Brand);
                SetComboText(ElectricalModelComboBox, data.Model);
                ElectricalSizeTextBox.Text = data.CapacityOrSize;
                ElectricalSerialTextBox.Text = data.ImeiOrSerial;
                ElectricalAccessoriesTextBox.Text = data.Accessories;
                ElectricalConditionTextBox.Text = data.Condition;
                break;

            default:
                OtherTypeTextBox.Text = data.ProductType;
                OtherBrandTextBox.Text = data.Brand;
                OtherModelTextBox.Text = data.Model;
                OtherDetailsTextBox.Text = data.OtherDetails;
                OtherSerialTextBox.Text = data.ImeiOrSerial;
                OtherAccessoriesTextBox.Text = data.Accessories;
                OtherConditionTextBox.Text = data.Condition;
                break;
        }
    }

    private string GetAssetCategoryName() =>
        (AssetCategoryComboBox.SelectedItem as ComboBoxItem)
            ?.Content?.ToString()?.Trim() ?? "อื่น ๆ";

    private string? GetCurrentProductType() => AssetCategoryComboBox.SelectedIndex switch
    {
        1 => GetComboText(ItTypeComboBox),
        2 => GetComboText(ElectricalTypeComboBox),
        3 => OtherTypeTextBox.Text,
        _ => null
    };

    private string? GetCurrentBrand() => AssetCategoryComboBox.SelectedIndex switch
    {
        0 => GetComboText(MobileBrandComboBox),
        1 => GetComboText(ItBrandComboBox),
        2 => GetComboText(ElectricalBrandComboBox),
        _ => OtherBrandTextBox.Text
    };

    private string? GetCurrentModel() => AssetCategoryComboBox.SelectedIndex switch
    {
        0 => GetComboText(MobileModelComboBox),
        1 => GetComboText(ItModelComboBox),
        2 => GetComboText(ElectricalModelComboBox),
        _ => OtherModelTextBox.Text
    };

    private string? GetCurrentCapacityOrSize() => AssetCategoryComboBox.SelectedIndex switch
    {
        0 => GetComboText(MobileCapacityComboBox),
        2 => ElectricalSizeTextBox.Text,
        _ => null
    };

    private string? GetCurrentColor() =>
        AssetCategoryComboBox.SelectedIndex == 0
            ? GetComboText(MobileColorComboBox)
            : null;

    private string? GetCurrentSerial() => AssetCategoryComboBox.SelectedIndex switch
    {
        0 => MobileImeiTextBox.Text,
        1 => ItSerialTextBox.Text,
        2 => ElectricalSerialTextBox.Text,
        _ => OtherSerialTextBox.Text
    };

    private string? GetCurrentAccessories() => AssetCategoryComboBox.SelectedIndex switch
    {
        0 => MobileAccessoriesTextBox.Text,
        1 => ItAccessoriesTextBox.Text,
        2 => ElectricalAccessoriesTextBox.Text,
        _ => OtherAccessoriesTextBox.Text
    };

    private string? GetCurrentCondition() => AssetCategoryComboBox.SelectedIndex switch
    {
        0 => MobileConditionTextBox.Text,
        1 => ItConditionTextBox.Text,
        2 => ElectricalConditionTextBox.Text,
        _ => OtherConditionTextBox.Text
    };

    private string? GetCurrentSpecification() =>
        AssetCategoryComboBox.SelectedIndex == 1
            ? ItSpecificationTextBox.Text
            : null;

    private string? GetCurrentOtherDetails() =>
        AssetCategoryComboBox.SelectedIndex == 3
            ? OtherDetailsTextBox.Text
            : null;

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

    private void SmartField_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(UpdateAssetPreview));
    }

    private void SmartField_KeyUp(
        object sender,
        KeyEventArgs e) => UpdateAssetPreview();

    private void SmartField_TextChanged(
        object sender,
        TextChangedEventArgs e) => UpdateAssetPreview();

    private void UpdateAssetPreview()
    {
        if (_isInitializing || AssetPreviewText is null)
        {
            return;
        }

        string summary = BuildProductSummary();
        AssetPreviewText.Text = string.IsNullOrWhiteSpace(summary)
            ? "กรอกข้อมูลสินค้า แล้วระบบจะสร้างรายละเอียดสรุปให้อัตโนมัติ"
            : summary;
    }

    private void FormComboBox_PreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not ComboBox comboBox || !comboBox.IsEnabled)
        {
            return;
        }

        if (!comboBox.IsDropDownOpen)
        {
            e.Handled = true;
            comboBox.IsDropDownOpen = true;
        }

        if (comboBox.IsEditable &&
            comboBox.Template.FindName("PART_EditableTextBox", comboBox)
                is TextBox editableTextBox)
        {
            editableTextBox.Focus();
            if (editableTextBox.SelectionLength == 0)
            {
                editableTextBox.CaretIndex =
                    editableTextBox.Text?.Length ?? 0;
            }
        }
    }

    private void LoadSmartLookupValues()
    {
        try
        {
            AddLearnedValues(MobileBrandComboBox, _pawnTicketService.GetSmartLookupValues("MobileTablet", "Brand"));
            AddLearnedValues(MobileModelComboBox, _pawnTicketService.GetSmartLookupValues("MobileTablet", "Model"));
            AddLearnedValues(MobileCapacityComboBox, _pawnTicketService.GetSmartLookupValues("MobileTablet", "Capacity"));
            AddLearnedValues(MobileColorComboBox, _pawnTicketService.GetSmartLookupValues("MobileTablet", "Color"));
            AddLearnedValues(ItTypeComboBox, _pawnTicketService.GetSmartLookupValues("IT", "ProductType"));
            AddLearnedValues(ItBrandComboBox, _pawnTicketService.GetSmartLookupValues("IT", "Brand"));
            AddLearnedValues(ItModelComboBox, _pawnTicketService.GetSmartLookupValues("IT", "Model"));
            AddLearnedValues(ElectricalTypeComboBox, _pawnTicketService.GetSmartLookupValues("Electrical", "ProductType"));
            AddLearnedValues(ElectricalBrandComboBox, _pawnTicketService.GetSmartLookupValues("Electrical", "Brand"));
            AddLearnedValues(ElectricalModelComboBox, _pawnTicketService.GetSmartLookupValues("Electrical", "Model"));
        }
        catch
        {
            // The form still works with its built-in values.
        }
    }

    private static void AddLearnedValues(
        ComboBox comboBox,
        IEnumerable<string> learnedValues)
    {
        HashSet<string> existing = comboBox.Items
            .Cast<object>()
            .Select(GetComboItemText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeLookupText)
            .ToHashSet();

        foreach (string value in learnedValues)
        {
            string normalized = NormalizeLookupText(value);
            if (string.IsNullOrWhiteSpace(normalized) ||
                existing.Contains(normalized))
            {
                continue;
            }

            comboBox.Items.Add(value);
            existing.Add(normalized);
        }
    }

    private List<SmartLookupEntry> BuildSmartLookupEntries()
    {
        List<SmartLookupEntry> entries = [];

        switch (AssetCategoryComboBox.SelectedIndex)
        {
            case 0:
                AddSmartLookup(entries, "MobileTablet", "Brand", GetComboText(MobileBrandComboBox));
                AddSmartLookup(entries, "MobileTablet", "Model", GetComboText(MobileModelComboBox));
                AddSmartLookup(entries, "MobileTablet", "Capacity", GetComboText(MobileCapacityComboBox));
                AddSmartLookup(entries, "MobileTablet", "Color", GetComboText(MobileColorComboBox));
                break;
            case 1:
                AddSmartLookup(entries, "IT", "ProductType", GetComboText(ItTypeComboBox));
                AddSmartLookup(entries, "IT", "Brand", GetComboText(ItBrandComboBox));
                AddSmartLookup(entries, "IT", "Model", GetComboText(ItModelComboBox));
                break;
            case 2:
                AddSmartLookup(entries, "Electrical", "ProductType", GetComboText(ElectricalTypeComboBox));
                AddSmartLookup(entries, "Electrical", "Brand", GetComboText(ElectricalBrandComboBox));
                AddSmartLookup(entries, "Electrical", "Model", GetComboText(ElectricalModelComboBox));
                break;
        }

        return entries;
    }

    private static void AddSmartLookup(
        ICollection<SmartLookupEntry> entries,
        string category,
        string fieldType,
        string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            entries.Add(new SmartLookupEntry(category, fieldType, value.Trim()));
        }
    }

    private static string GetComboItemText(object item) =>
        item is ComboBoxItem comboBoxItem
            ? comboBoxItem.Content?.ToString()?.Trim() ?? string.Empty
            : item?.ToString()?.Trim() ?? string.Empty;

    private static string NormalizeLookupText(string value) =>
        string.Join(
            " ",
            value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .ToUpperInvariant();

    private void PurchaseDatePicker_PreviewMouseLeftButtonUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (sender is not DatePicker datePicker)
        {
            return;
        }

        DatePickerTextBox? textBox = GetDatePickerTextBox(datePicker);
        if (textBox is null || !textBox.IsMouseOver)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => SelectDatePartAtCaret(textBox, textBox.CaretIndex)));
    }

    private void PurchaseDatePicker_PreviewKeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (sender is not DatePicker datePicker ||
            (e.Key != Key.Left && e.Key != Key.Right) ||
            Keyboard.Modifiers != ModifierKeys.None)
        {
            return;
        }

        DatePickerTextBox? textBox = GetDatePickerTextBox(datePicker);
        if (textBox is null)
        {
            return;
        }

        List<(int Start, int Length)> parts = GetDatePartRanges(textBox.Text);
        int current = FindDatePartIndex(parts, textBox.SelectionStart);
        int next = e.Key == Key.Right ? current + 1 : current - 1;
        if (current < 0 || next < 0 || next >= parts.Count)
        {
            return;
        }

        (int start, int length) = parts[next];
        e.Handled = true;
        textBox.Focus();
        textBox.Select(start, length);
    }

    private static DatePickerTextBox? GetDatePickerTextBox(DatePicker datePicker)
    {
        datePicker.ApplyTemplate();
        return datePicker.Template.FindName("PART_TextBox", datePicker)
            as DatePickerTextBox;
    }

    private static void SelectDatePartAtCaret(
        DatePickerTextBox textBox,
        int caretIndex)
    {
        List<(int Start, int Length)> parts = GetDatePartRanges(textBox.Text);
        int index = FindDatePartIndex(parts, caretIndex);
        if (index < 0)
        {
            return;
        }

        (int start, int length) = parts[index];
        textBox.Focus();
        textBox.Select(start, length);
    }

    private static int FindDatePartIndex(
        IReadOnlyList<(int Start, int Length)> parts,
        int textPosition)
    {
        for (int index = 0; index < parts.Count; index++)
        {
            (int start, int length) = parts[index];
            if (textPosition >= start && textPosition <= start + length)
            {
                return index;
            }
        }

        return -1;
    }

    private static List<(int Start, int Length)> GetDatePartRanges(string? text)
    {
        List<(int Start, int Length)> parts = [];
        string value = text ?? string.Empty;
        int index = 0;

        while (index < value.Length)
        {
            if (!char.IsDigit(value[index]))
            {
                index++;
                continue;
            }

            int start = index;
            while (index < value.Length && char.IsDigit(value[index]))
            {
                index++;
            }

            parts.Add((start, index - start));
        }

        return parts;
    }

    private static int? ParseAge(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (!int.TryParse(text.Trim(), out int age)) throw new InvalidOperationException("อายุต้องเป็นตัวเลข");
        return age;
    }

    private static bool TryParseAmount(string text, out decimal value) =>
        decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
        decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out value);

    private static string GetComboText(ComboBox comboBox) =>
        !string.IsNullOrWhiteSpace(comboBox.Text)
            ? comboBox.Text.Trim()
            : (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? string.Empty;

    private static void SetComboText(ComboBox comboBox, string value)
    {
        foreach (object item in comboBox.Items)
        {
            if (item is ComboBoxItem comboItem && string.Equals(comboItem.Content?.ToString(), value, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
        comboBox.Text = value;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
