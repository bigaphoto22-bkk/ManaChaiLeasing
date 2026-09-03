using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class DirectPurchaseListRow
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = "-";
    public DateTime PurchaseDate { get; init; }
    public string PurchaseDateText => PurchaseDate.ToString("dd/MM/yyyy");
    public string SellerName { get; init; } = string.Empty;
    public string ProductSummary { get; init; } = string.Empty;
    public decimal PurchasePrice { get; init; }
    public string PurchasePriceText => $"{PurchasePrice:N2}";
    public DateTime? SaleDate { get; init; }
    public string SaleDateText => SaleDate?.ToString("dd/MM/yyyy") ?? "-";
    public decimal? SalePrice { get; init; }
    public string SalePriceText => SalePrice.HasValue ? $"{SalePrice.Value:N2}" : "-";
    public decimal? Profit => SalePrice.HasValue ? SalePrice.Value - PurchasePrice : null;
    public string ProfitText => Profit.HasValue ? $"{Profit.Value:N2}" : "-";
    public bool HasProfit => Profit.HasValue;
    public bool IsLoss => Profit < 0m;
    public string PaymentMethod { get; init; } = "-";
    public DirectPurchaseStatus Status { get; init; }
    public string StatusText => DirectPurchaseService.StatusText(Status);
}

public sealed class DirectPurchaseData
{
    public int Id { get; init; }
    public string DocumentNumber { get; init; } = string.Empty;
    public DateTime PurchaseDate { get; init; }
    public decimal PurchasePrice { get; init; }
    public DirectPurchaseStatus Status { get; init; }
    public int SellerCustomerId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string CitizenId { get; init; } = string.Empty;
    public int? Age { get; init; }
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string AssetCategory { get; init; } = string.Empty;
    public string ProductType { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string CapacityOrSize { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
    public string ImeiOrSerial { get; init; } = string.Empty;
    public string Accessories { get; init; } = string.Empty;
    public string Condition { get; init; } = string.Empty;
    public string Specification { get; init; } = string.Empty;
    public string OtherDetails { get; init; } = string.Empty;
    public string ProductSummary { get; init; } = string.Empty;
    public string PaymentMethod { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public string CancellationReason { get; init; } = string.Empty;
    public DateTime? SaleDate { get; init; }
    public decimal? SalePrice { get; init; }
    public decimal? SaleProfit => SalePrice.HasValue ? SalePrice.Value - PurchasePrice : null;
    public string SalePaymentMethod { get; init; } = string.Empty;
    public string SaleNote { get; init; } = string.Empty;
}

public sealed class DirectPurchaseSalePreview
{
    public int DirectPurchaseId { get; init; }
    public string DocumentNumber { get; init; } = "-";
    public DateTime PurchaseDate { get; init; }
    public decimal PurchasePrice { get; init; }
    public string SellerName { get; init; } = string.Empty;
    public string ProductSummary { get; init; } = string.Empty;
    public bool IsEditing { get; init; }
    public DateTime? SaleDate { get; init; }
    public decimal? SalePrice { get; init; }
    public string SalePaymentMethod { get; init; } = string.Empty;
    public string SaleNote { get; init; } = string.Empty;
}

public sealed class DirectPurchaseSaleResult
{
    public int DirectPurchaseId { get; init; }
    public string DocumentNumber { get; init; } = "-";
    public DateTime SaleDate { get; init; }
    public decimal PurchasePrice { get; init; }
    public decimal SalePrice { get; init; }
    public decimal Profit => SalePrice - PurchasePrice;
}

public sealed class DirectPurchaseSaveRequest
{
    public int? Id { get; init; }
    public int? SelectedSellerCustomerId { get; init; }
    public string? DocumentNumber { get; init; }
    public DateTime PurchaseDate { get; init; }
    public decimal PurchasePrice { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? CitizenId { get; init; }
    public int? Age { get; init; }
    public string? Phone { get; init; }
    public string? Address { get; init; }
    public string AssetCategory { get; init; } = string.Empty;
    public string? ProductType { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public string? CapacityOrSize { get; init; }
    public string? Color { get; init; }
    public string? ImeiOrSerial { get; init; }
    public string? Accessories { get; init; }
    public string? Condition { get; init; }
    public string? Specification { get; init; }
    public string? OtherDetails { get; init; }
    public string ProductSummary { get; init; } = string.Empty;
    public string? PaymentMethod { get; init; }
    public string? Note { get; init; }
    public string? EditReason { get; init; }
    public IReadOnlyCollection<SmartLookupEntry> SmartLookupValues { get; init; }
        = Array.Empty<SmartLookupEntry>();
}

public sealed class DirectPurchaseService
{
    private sealed record FieldChange(string Label, string OldValue, string NewValue);

    public List<DirectPurchaseListRow> Search(string? keyword, DirectPurchaseStatus? status)
    {
        using AppDbContext db = new();
        IQueryable<DirectPurchase> query = db.DirectPurchases
            .AsNoTracking()
            .Include(item => item.SellerCustomer);

        if (status.HasValue)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        string term = keyword?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(term))
        {
            string pattern = $"%{term}%";
            query = query.Where(item =>
                (item.DocumentNumber != null && EF.Functions.Like(item.DocumentNumber, pattern)) ||
                EF.Functions.Like(item.SellerCustomer.FirstName, pattern) ||
                EF.Functions.Like(item.SellerCustomer.LastName, pattern) ||
                EF.Functions.Like(item.SellerCustomer.FirstName + " " + item.SellerCustomer.LastName, pattern) ||
                (item.SellerCustomer.CitizenId != null && EF.Functions.Like(item.SellerCustomer.CitizenId, pattern)) ||
                (item.SellerCustomer.Phone != null && EF.Functions.Like(item.SellerCustomer.Phone, pattern)) ||
                EF.Functions.Like(item.ProductSummary, pattern) ||
                (item.ImeiOrSerial != null && EF.Functions.Like(item.ImeiOrSerial, pattern)));
        }

        return query
            .OrderByDescending(item => item.PurchaseDate)
            .ThenByDescending(item => item.Id)
            .Take(500)
            .Select(item => new DirectPurchaseListRow
            {
                Id = item.Id,
                DocumentNumber = item.DocumentNumber ?? "-",
                PurchaseDate = item.PurchaseDate,
                SellerName = (item.SellerCustomer.FirstName + " " + item.SellerCustomer.LastName).Trim(),
                ProductSummary = item.ProductSummary,
                PurchasePrice = item.PurchasePrice,
                SaleDate = item.Transactions
                    .Where(transaction =>
                        !transaction.IsVoided &&
                        transaction.TransactionType == DirectPurchaseTransactionType.Sale)
                    .Select(transaction => (DateTime?)transaction.TransactionDate)
                    .FirstOrDefault(),
                SalePrice = item.Transactions
                    .Where(transaction =>
                        !transaction.IsVoided &&
                        transaction.TransactionType == DirectPurchaseTransactionType.Sale)
                    .Select(transaction => (decimal?)transaction.Amount)
                    .FirstOrDefault(),
                PaymentMethod = item.PaymentMethod ?? "-",
                Status = item.Status
            })
            .ToList();
    }

    public DirectPurchaseData Get(int id)
    {
        using AppDbContext db = new();
        DirectPurchase item = db.DirectPurchases
            .AsNoTracking()
            .Include(value => value.SellerCustomer)
            .Include(value => value.Transactions)
            .SingleOrDefault(value => value.Id == id)
            ?? throw new InvalidOperationException("ไม่พบรายการรับซื้อที่ต้องการ");

        DirectPurchaseTransaction? saleTransaction = item.Transactions
            .Where(value =>
                !value.IsVoided &&
                value.TransactionType == DirectPurchaseTransactionType.Sale)
            .OrderByDescending(value => value.TransactionDate)
            .ThenByDescending(value => value.Id)
            .FirstOrDefault();

        return new DirectPurchaseData
        {
            Id = item.Id,
            DocumentNumber = item.DocumentNumber ?? string.Empty,
            PurchaseDate = item.PurchaseDate,
            PurchasePrice = item.PurchasePrice,
            Status = item.Status,
            SellerCustomerId = item.SellerCustomerId,
            FirstName = item.SellerCustomer.FirstName,
            LastName = item.SellerCustomer.LastName,
            CitizenId = item.SellerCustomer.CitizenId ?? string.Empty,
            Age = item.SellerCustomer.Age,
            Phone = item.SellerCustomer.Phone ?? string.Empty,
            Address = item.SellerCustomer.Address ?? string.Empty,
            AssetCategory = item.AssetCategory,
            ProductType = item.ProductType ?? string.Empty,
            Brand = item.Brand ?? string.Empty,
            Model = item.Model ?? string.Empty,
            CapacityOrSize = item.CapacityOrSize ?? string.Empty,
            Color = item.Color ?? string.Empty,
            ImeiOrSerial = item.ImeiOrSerial ?? string.Empty,
            Accessories = item.Accessories ?? string.Empty,
            Condition = item.Condition ?? string.Empty,
            Specification = item.Specification ?? string.Empty,
            OtherDetails = item.OtherDetails ?? string.Empty,
            ProductSummary = item.ProductSummary,
            PaymentMethod = item.PaymentMethod ?? string.Empty,
            Note = item.Note ?? string.Empty,
            CancellationReason = item.CancellationReason ?? string.Empty,
            SaleDate = saleTransaction?.TransactionDate,
            SalePrice = saleTransaction?.Amount,
            SalePaymentMethod = saleTransaction?.PaymentMethod ?? string.Empty,
            SaleNote = saleTransaction?.Note ?? string.Empty
        };
    }

    public DirectPurchaseSalePreview GetSalePreview(int id)
    {
        using AppDbContext db = new();
        DirectPurchase item = db.DirectPurchases
            .AsNoTracking()
            .Include(value => value.SellerCustomer)
            .SingleOrDefault(value => value.Id == id)
            ?? throw new InvalidOperationException("ไม่พบรายการรับซื้อที่ต้องการขาย");

        if (item.Status != DirectPurchaseStatus.InStock)
        {
            throw new InvalidOperationException("ขายได้เฉพาะรายการที่มีสถานะรอขายเท่านั้น");
        }

        return new DirectPurchaseSalePreview
        {
            DirectPurchaseId = item.Id,
            DocumentNumber = Display(item.DocumentNumber),
            PurchaseDate = item.PurchaseDate,
            PurchasePrice = item.PurchasePrice,
            SellerName = $"{item.SellerCustomer.FirstName} {item.SellerCustomer.LastName}".Trim(),
            ProductSummary = item.ProductSummary,
            IsEditing = false
        };
    }

    public DirectPurchaseSalePreview GetSaleEditPreview(int id)
    {
        using AppDbContext db = new();
        DirectPurchase item = db.DirectPurchases
            .AsNoTracking()
            .Include(value => value.SellerCustomer)
            .Include(value => value.Transactions)
            .SingleOrDefault(value => value.Id == id)
            ?? throw new InvalidOperationException("ไม่พบรายการขายที่ต้องการแก้ไข");

        if (item.Status != DirectPurchaseStatus.Sold)
            throw new InvalidOperationException("แก้ไขข้อมูลการขายได้เฉพาะรายการสถานะขายแล้ว");

        DirectPurchaseTransaction saleTransaction = item.Transactions
            .Where(value =>
                !value.IsVoided &&
                value.TransactionType == DirectPurchaseTransactionType.Sale)
            .OrderByDescending(value => value.TransactionDate)
            .ThenByDescending(value => value.Id)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("ไม่พบ Transaction การขายของรายการนี้");

        return new DirectPurchaseSalePreview
        {
            DirectPurchaseId = item.Id,
            DocumentNumber = Display(item.DocumentNumber),
            PurchaseDate = item.PurchaseDate,
            PurchasePrice = item.PurchasePrice,
            SellerName = $"{item.SellerCustomer.FirstName} {item.SellerCustomer.LastName}".Trim(),
            ProductSummary = item.ProductSummary,
            IsEditing = true,
            SaleDate = saleTransaction.TransactionDate,
            SalePrice = saleTransaction.Amount,
            SalePaymentMethod = saleTransaction.PaymentMethod ?? string.Empty,
            SaleNote = saleTransaction.Note ?? string.Empty
        };
    }

    public DirectPurchaseSaleResult SaveSale(
        int id,
        DateTime saleDate,
        decimal salePrice,
        string? paymentMethod,
        string? note)
    {
        DateTime normalizedSaleDate = saleDate.Date;
        string cleanedPaymentMethod = Required(paymentMethod, "ช่องทางการรับเงิน");
        string? cleanedNote = Clean(note);

        if (normalizedSaleDate > DateTime.Today)
            throw new InvalidOperationException("วันที่ขายต้องไม่เกินวันนี้");
        if (salePrice <= 0m)
            throw new InvalidOperationException("ราคาขายต้องมากกว่า 0 บาท");

        Max(cleanedPaymentMethod, 50, "ช่องทางการรับเงิน");
        Max(cleanedNote, 1000, "หมายเหตุการขาย");

        lock (BusinessTransactionGate.SyncRoot)
        {
            using AppDbContext db = new();
            using var transaction = db.Database.BeginTransaction();
            DirectPurchase item = db.DirectPurchases
                .Include(value => value.Transactions)
                .Include(value => value.EditAudits)
                .SingleOrDefault(value => value.Id == id)
                ?? throw new InvalidOperationException("ไม่พบรายการรับซื้อที่ต้องการขาย");

            if (item.Status != DirectPurchaseStatus.InStock)
                throw new InvalidOperationException("ขายได้เฉพาะรายการที่มีสถานะรอขายเท่านั้น");
            if (normalizedSaleDate < item.PurchaseDate.Date)
                throw new InvalidOperationException("วันที่ขายต้องไม่ก่อนวันที่รับซื้อ");
            if (item.Transactions.Any(value =>
                    !value.IsVoided &&
                    value.TransactionType == DirectPurchaseTransactionType.Sale))
                throw new InvalidOperationException("รายการนี้บันทึกขายแล้ว");

            DateTime now = DateTime.Now;
            item.Status = DirectPurchaseStatus.Sold;
            item.UpdatedAt = now;

            item.Transactions.Add(new DirectPurchaseTransaction
            {
                TransactionType = DirectPurchaseTransactionType.Sale,
                CashFlowType = CashFlowType.Income,
                TransactionDate = normalizedSaleDate.Add(now.TimeOfDay),
                Amount = salePrice,
                PaymentMethod = cleanedPaymentMethod,
                Note = cleanedNote,
                CreatedAt = now
            });

            decimal profit = salePrice - item.PurchasePrice;
            item.EditAudits.Add(new DirectPurchaseEditAudit
            {
                EditedAt = now,
                EditorUser = Environment.UserName,
                EditorMachine = Environment.MachineName,
                Reason = "บันทึกขายสินค้า",
                ChangeSummary =
                    "สถานะ: รอขาย → ขายแล้ว" + Environment.NewLine +
                    $"วันที่ขาย: {normalizedSaleDate:dd/MM/yyyy}" + Environment.NewLine +
                    $"ราคาขาย: {salePrice:N2} บาท" + Environment.NewLine +
                    $"กำไร/ขาดทุน: {profit:N2} บาท"
            });

            db.SaveChanges();
            transaction.Commit();

            return new DirectPurchaseSaleResult
            {
                DirectPurchaseId = item.Id,
                DocumentNumber = Display(item.DocumentNumber),
                SaleDate = normalizedSaleDate,
                PurchasePrice = item.PurchasePrice,
                SalePrice = salePrice
            };
        }
    }

    public DirectPurchaseSaleResult UpdateSale(
        int id,
        DateTime saleDate,
        decimal salePrice,
        string? paymentMethod,
        string? note,
        string? editReason)
    {
        DateTime normalizedSaleDate = saleDate.Date;
        string cleanedPaymentMethod = Required(paymentMethod, "ช่องทางการรับเงิน");
        string? cleanedNote = Clean(note);
        string? cleanedReason = Clean(editReason);

        if (normalizedSaleDate > DateTime.Today)
            throw new InvalidOperationException("วันที่ขายต้องไม่เกินวันนี้");
        if (salePrice <= 0m)
            throw new InvalidOperationException("ราคาขายต้องมากกว่า 0 บาท");

        Max(cleanedPaymentMethod, 50, "ช่องทางการรับเงิน");
        Max(cleanedNote, 1000, "หมายเหตุการขาย");
        Max(cleanedReason, 1000, "เหตุผลการแก้ไข");

        lock (BusinessTransactionGate.SyncRoot)
        {
            using AppDbContext db = new();
            using var transaction = db.Database.BeginTransaction();
            DirectPurchase item = db.DirectPurchases
                .Include(value => value.Transactions)
                .Include(value => value.EditAudits)
                .SingleOrDefault(value => value.Id == id)
                ?? throw new InvalidOperationException("ไม่พบรายการขายที่ต้องการแก้ไข");

            if (item.Status != DirectPurchaseStatus.Sold)
                throw new InvalidOperationException("แก้ไขข้อมูลการขายได้เฉพาะรายการสถานะขายแล้ว");
            if (normalizedSaleDate < item.PurchaseDate.Date)
                throw new InvalidOperationException("วันที่ขายต้องไม่ก่อนวันที่รับซื้อ");

            DirectPurchaseTransaction saleTransaction = item.Transactions
                .Where(value =>
                    !value.IsVoided &&
                    value.TransactionType == DirectPurchaseTransactionType.Sale)
                .OrderByDescending(value => value.TransactionDate)
                .ThenByDescending(value => value.Id)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("ไม่พบ Transaction การขายของรายการนี้");

            List<FieldChange> changes = [];
            decimal oldProfit = saleTransaction.Amount - item.PurchasePrice;
            decimal newProfit = salePrice - item.PurchasePrice;
            Track(changes, "วันที่ขาย", saleTransaction.TransactionDate.ToString("dd/MM/yyyy"), normalizedSaleDate.ToString("dd/MM/yyyy"));
            Track(changes, "ราคาขาย", saleTransaction.Amount.ToString("N2"), salePrice.ToString("N2"));
            Track(changes, "ช่องทางการรับเงิน", saleTransaction.PaymentMethod, cleanedPaymentMethod);
            Track(changes, "หมายเหตุการขาย", saleTransaction.Note, cleanedNote);
            Track(changes, "กำไร/ขาดทุน", oldProfit.ToString("N2"), newProfit.ToString("N2"));

            if (changes.Count == 0)
                throw new InvalidOperationException("ไม่มีข้อมูลการขายที่เปลี่ยนแปลง");

            TimeSpan originalTime = saleTransaction.TransactionDate.TimeOfDay;
            saleTransaction.TransactionDate = normalizedSaleDate.Add(originalTime);
            saleTransaction.Amount = salePrice;
            saleTransaction.PaymentMethod = cleanedPaymentMethod;
            saleTransaction.Note = cleanedNote;

            DateTime now = DateTime.Now;
            item.UpdatedAt = now;
            item.EditAudits.Add(new DirectPurchaseEditAudit
            {
                EditedAt = now,
                EditorUser = Environment.UserName,
                EditorMachine = Environment.MachineName,
                Reason = cleanedReason ?? "ไม่ได้ระบุ",
                ChangeSummary = string.Join(Environment.NewLine, changes.Select(change =>
                    $"{change.Label}: {Display(change.OldValue)} → {Display(change.NewValue)}"))
            });

            db.SaveChanges();
            transaction.Commit();

            return new DirectPurchaseSaleResult
            {
                DirectPurchaseId = item.Id,
                DocumentNumber = Display(item.DocumentNumber),
                SaleDate = normalizedSaleDate,
                PurchasePrice = item.PurchasePrice,
                SalePrice = salePrice
            };
        }
    }

    public int Save(DirectPurchaseSaveRequest request)
    {
        lock (BusinessTransactionGate.SyncRoot)
        {
            using AppDbContext db = new();
            using var transaction = db.Database.BeginTransaction();

            ValidatedInput input = Validate(request);
            DirectPurchase? item = request.Id.HasValue
                ? db.DirectPurchases
                    .Include(value => value.SellerCustomer)
                    .Include(value => value.Transactions)
                    .SingleOrDefault(value => value.Id == request.Id.Value)
                : null;

            if (request.Id.HasValue && item is null)
            {
                throw new InvalidOperationException("ไม่พบรายการรับซื้อที่ต้องการแก้ไข");
            }

            if (item is not null && item.Status != DirectPurchaseStatus.InStock)
            {
                throw new InvalidOperationException("แก้ไขได้เฉพาะรายการที่มีสถานะรอขายเท่านั้น");
            }

            bool duplicateDocument = input.DocumentNumber is not null &&
                db.DirectPurchases.AsNoTracking().Any(value =>
                    (!request.Id.HasValue || value.Id != request.Id.Value) &&
                    value.DocumentNumber != null &&
                    value.DocumentNumber.ToUpper() == input.DocumentNumber.ToUpper());

            if (duplicateDocument)
            {
                throw new InvalidOperationException($"เลขที่เอกสารรับซื้อ {input.DocumentNumber} มีอยู่ในระบบแล้ว");
            }

            Customer seller = ResolveSeller(db, request, item);
            List<FieldChange> changes = [];
            DateTime now = DateTime.Now;

            if (item is null)
            {
                item = new DirectPurchase
                {
                    CreatedAt = now,
                    Status = DirectPurchaseStatus.InStock,
                    SellerCustomer = seller
                };
                db.DirectPurchases.Add(item);
            }
            else
            {
                Track(changes, "เลขที่เอกสารรับซื้อ", item.DocumentNumber, input.DocumentNumber);
                Track(changes, "วันที่รับซื้อ", item.PurchaseDate.ToString("dd/MM/yyyy"), input.PurchaseDate.ToString("dd/MM/yyyy"));
                Track(changes, "ราคารับซื้อ", item.PurchasePrice.ToString("N2"), input.PurchasePrice.ToString("N2"));
                Track(changes, "ชื่อผู้ขาย", item.SellerCustomer.FirstName, input.FirstName);
                Track(changes, "นามสกุลผู้ขาย", item.SellerCustomer.LastName, input.LastName);
                Track(changes, "เลขบัตรประชาชน", item.SellerCustomer.CitizenId, input.CitizenId);
                Track(changes, "อายุ", item.SellerCustomer.Age?.ToString(), input.Age?.ToString());
                Track(changes, "โทรศัพท์", item.SellerCustomer.Phone, input.Phone);
                Track(changes, "ที่อยู่", item.SellerCustomer.Address, input.Address);
                Track(changes, "ประเภทหลัก", item.AssetCategory, input.AssetCategory);
                Track(changes, "ชนิดสินค้า", item.ProductType, input.ProductType);
                Track(changes, "ยี่ห้อ", item.Brand, input.Brand);
                Track(changes, "รุ่น", item.Model, input.Model);
                Track(changes, "ความจุ / ขนาด", item.CapacityOrSize, input.CapacityOrSize);
                Track(changes, "สี", item.Color, input.Color);
                Track(changes, "IMEI / Serial", item.ImeiOrSerial, input.ImeiOrSerial);
                Track(changes, "อุปกรณ์", item.Accessories, input.Accessories);
                Track(changes, "สภาพ / ตำหนิ", item.Condition, input.Condition);
                Track(changes, "สเปก", item.Specification, input.Specification);
                Track(changes, "รายละเอียดอื่น ๆ", item.OtherDetails, input.OtherDetails);
                Track(changes, "รายละเอียดสินค้า", item.ProductSummary, input.ProductSummary);
                Track(changes, "ช่องทางชำระ", item.PaymentMethod, input.PaymentMethod);
                Track(changes, "หมายเหตุ", item.Note, input.Note);
            }

            ApplySeller(seller, input, now);
            ApplyItem(item, input, seller, now);

            DirectPurchaseTransaction? purchaseTransaction = item.Transactions
                .FirstOrDefault(value => value.TransactionType == DirectPurchaseTransactionType.Purchase);

            if (purchaseTransaction is null)
            {
                purchaseTransaction = new DirectPurchaseTransaction
                {
                    DirectPurchase = item,
                    TransactionType = DirectPurchaseTransactionType.Purchase,
                    CashFlowType = CashFlowType.Expense,
                    CreatedAt = now
                };
                db.DirectPurchaseTransactions.Add(purchaseTransaction);
            }

            TimeSpan transactionTime = purchaseTransaction.Id == 0
                ? now.TimeOfDay
                : purchaseTransaction.TransactionDate.TimeOfDay;
            purchaseTransaction.TransactionDate =
                input.PurchaseDate.Date.Add(transactionTime);
            purchaseTransaction.Amount = input.PurchasePrice;
            purchaseTransaction.PaymentMethod = input.PaymentMethod;
            purchaseTransaction.Note = input.Note;
            purchaseTransaction.IsVoided = false;
            purchaseTransaction.VoidReason = null;

            if (request.Id.HasValue)
            {
                if (changes.Count == 0)
                {
                    throw new InvalidOperationException("ไม่มีข้อมูลที่เปลี่ยนแปลง");
                }

                item.EditAudits.Add(new DirectPurchaseEditAudit
                {
                    EditedAt = now,
                    EditorUser = Environment.UserName,
                    EditorMachine = Environment.MachineName,
                    Reason = Clean(request.EditReason) ?? "ไม่ได้ระบุ",
                    ChangeSummary = string.Join(Environment.NewLine, changes.Select(change =>
                        $"{change.Label}: {Display(change.OldValue)} → {Display(change.NewValue)}"))
                });
            }

            LearnSmartLookupValues(
                db,
                request.SmartLookupValues,
                now);

            db.SaveChanges();
            transaction.Commit();
            return item.Id;
        }
    }

    public void Cancel(int id, string reason)
    {
        string cleanedReason = Clean(reason)
            ?? throw new InvalidOperationException("กรุณาระบุเหตุผลการยกเลิกรายการ");
        if (cleanedReason.Length > 1000)
        {
            throw new InvalidOperationException("เหตุผลการยกเลิกยาวเกิน 1,000 ตัวอักษร");
        }

        lock (BusinessTransactionGate.SyncRoot)
        {
            using AppDbContext db = new();
            using var transaction = db.Database.BeginTransaction();
            DirectPurchase item = db.DirectPurchases
                .Include(value => value.Transactions)
                .SingleOrDefault(value => value.Id == id)
                ?? throw new InvalidOperationException("ไม่พบรายการรับซื้อที่ต้องการยกเลิก");

            if (item.Status != DirectPurchaseStatus.InStock)
            {
                throw new InvalidOperationException("ยกเลิกได้เฉพาะรายการที่มีสถานะรอขายเท่านั้น");
            }

            DateTime now = DateTime.Now;
            item.Status = DirectPurchaseStatus.Cancelled;
            item.CancellationReason = cleanedReason;
            item.CancelledAt = now;
            item.UpdatedAt = now;

            foreach (DirectPurchaseTransaction value in item.Transactions.Where(value => !value.IsVoided))
            {
                value.IsVoided = true;
                value.VoidReason = $"ยกเลิกรายการรับซื้อ: {cleanedReason}";
            }

            item.EditAudits.Add(new DirectPurchaseEditAudit
            {
                EditedAt = now,
                EditorUser = Environment.UserName,
                EditorMachine = Environment.MachineName,
                Reason = cleanedReason,
                ChangeSummary = "สถานะ: รอขาย → ยกเลิก"
            });

            db.SaveChanges();
            transaction.Commit();
        }
    }

    public static string StatusText(DirectPurchaseStatus status) => status switch
    {
        DirectPurchaseStatus.InStock => "รอขาย",
        DirectPurchaseStatus.Sold => "ขายแล้ว",
        DirectPurchaseStatus.Cancelled => "ยกเลิก",
        _ => status.ToString()
    };

    private static Customer ResolveSeller(AppDbContext db, DirectPurchaseSaveRequest request, DirectPurchase? item)
    {
        int? sellerId = request.SelectedSellerCustomerId ?? item?.SellerCustomerId;
        Customer? seller = sellerId.HasValue
            ? db.Customers.SingleOrDefault(value => value.Id == sellerId.Value)
            : null;

        if (sellerId.HasValue && seller is null)
        {
            throw new InvalidOperationException("ไม่พบข้อมูลผู้ขายที่เลือก กรุณาค้นหาใหม่อีกครั้ง");
        }

        string? citizenId = Clean(request.CitizenId);
        if (citizenId is not null)
        {
            Customer? duplicate = db.Customers.SingleOrDefault(value =>
                value.CitizenId == citizenId &&
                (!sellerId.HasValue || value.Id != sellerId.Value));

            if (duplicate is not null)
            {
                throw new InvalidOperationException("เลขบัตรประชาชนนี้มีข้อมูลลูกค้าอยู่แล้ว กรุณาใช้ปุ่มค้นหาลูกค้าเก่า");
            }
        }

        if (seller is null)
        {
            seller = new Customer { CreatedAt = DateTime.Now };
            db.Customers.Add(seller);
        }

        return seller;
    }

    private sealed record ValidatedInput(
        string? DocumentNumber, DateTime PurchaseDate, decimal PurchasePrice,
        string FirstName, string LastName, string? CitizenId, int? Age,
        string? Phone, string? Address, string AssetCategory, string? ProductType,
        string? Brand, string? Model, string? CapacityOrSize, string? Color,
        string? ImeiOrSerial, string? Accessories, string? Condition,
        string? Specification, string? OtherDetails, string ProductSummary,
        string? PaymentMethod, string? Note);

    private static ValidatedInput Validate(DirectPurchaseSaveRequest request)
    {
        string? documentNumber = Clean(request.DocumentNumber);
        DateTime purchaseDate = request.PurchaseDate.Date;
        string firstName = Required(request.FirstName, "ชื่อผู้ขาย");
        string lastName = Required(request.LastName, "นามสกุลผู้ขาย");
        string assetCategory = Required(request.AssetCategory, "ประเภทสินค้า");
        string productSummary = Required(request.ProductSummary, "รายละเอียดสินค้า");
        string? citizenId = Clean(request.CitizenId);

        if (purchaseDate > DateTime.Today) throw new InvalidOperationException("วันที่รับซื้อต้องไม่เกินวันนี้");
        if (request.PurchasePrice <= 0m) throw new InvalidOperationException("ราคารับซื้อต้องมากกว่า 0 บาท");
        if (citizenId is not null && (citizenId.Length != 13 || !citizenId.All(char.IsDigit)))
            throw new InvalidOperationException("เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก");
        if (request.Age.HasValue && (request.Age < 1 || request.Age > 150))
            throw new InvalidOperationException("อายุต้องอยู่ระหว่าง 1 ถึง 150 ปี");

        Max(documentNumber, 50, "เลขที่เอกสารรับซื้อ"); Max(firstName, 100, "ชื่อผู้ขาย");
        Max(lastName, 100, "นามสกุลผู้ขาย"); Max(citizenId, 13, "เลขบัตรประชาชน");
        Max(request.Phone, 30, "โทรศัพท์"); Max(request.Address, 1000, "ที่อยู่");
        Max(assetCategory, 100, "ประเภทสินค้า"); Max(request.ProductType, 100, "ชนิดสินค้า");
        Max(request.Brand, 100, "ยี่ห้อ"); Max(request.Model, 200, "รุ่น");
        Max(request.CapacityOrSize, 100, "ความจุ / ขนาด"); Max(request.Color, 100, "สี");
        Max(request.ImeiOrSerial, 150, "IMEI / Serial"); Max(request.Accessories, 500, "อุปกรณ์");
        Max(request.Condition, 1000, "สภาพ / ตำหนิ"); Max(request.Specification, 1500, "สเปก");
        Max(request.OtherDetails, 1500, "รายละเอียดอื่น ๆ"); Max(productSummary, 2500, "รายละเอียดสินค้า");
        Max(request.PaymentMethod, 50, "ช่องทางชำระ"); Max(request.Note, 1500, "หมายเหตุ");
        Max(request.EditReason, 1000, "เหตุผลการแก้ไข");

        return new ValidatedInput(documentNumber, purchaseDate, request.PurchasePrice,
            firstName, lastName, citizenId, request.Age, Clean(request.Phone), Clean(request.Address),
            assetCategory, Clean(request.ProductType), Clean(request.Brand), Clean(request.Model),
            Clean(request.CapacityOrSize), Clean(request.Color), Clean(request.ImeiOrSerial),
            Clean(request.Accessories), Clean(request.Condition), Clean(request.Specification),
            Clean(request.OtherDetails), productSummary, Clean(request.PaymentMethod), Clean(request.Note));
    }

    private static void ApplySeller(Customer seller, ValidatedInput input, DateTime now)
    {
        seller.FirstName = input.FirstName; seller.LastName = input.LastName;
        seller.CitizenId = input.CitizenId; seller.Age = input.Age;
        seller.Phone = input.Phone; seller.Address = input.Address; seller.UpdatedAt = now;
    }

    private static void ApplyItem(DirectPurchase item, ValidatedInput input, Customer seller, DateTime now)
    {
        item.DocumentNumber = input.DocumentNumber; item.PurchaseDate = input.PurchaseDate;
        item.PurchasePrice = input.PurchasePrice; item.SellerCustomer = seller;
        item.AssetCategory = input.AssetCategory; item.ProductType = input.ProductType;
        item.Brand = input.Brand; item.Model = input.Model; item.CapacityOrSize = input.CapacityOrSize;
        item.Color = input.Color; item.ImeiOrSerial = input.ImeiOrSerial; item.Accessories = input.Accessories;
        item.Condition = input.Condition; item.Specification = input.Specification;
        item.OtherDetails = input.OtherDetails; item.ProductSummary = input.ProductSummary;
        item.PaymentMethod = input.PaymentMethod; item.Note = input.Note; item.UpdatedAt = now;
    }

    private static void Track(List<FieldChange> changes, string label, string? oldValue, string? newValue)
    {
        if (!string.Equals(Clean(oldValue), Clean(newValue), StringComparison.Ordinal))
            changes.Add(new FieldChange(label, Clean(oldValue) ?? string.Empty, Clean(newValue) ?? string.Empty));
    }

    private static string Required(string? value, string label) =>
        Clean(value) ?? throw new InvalidOperationException($"กรุณากรอก{label}");

    private static string? Clean(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
    }

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "-" : value;

    private static void LearnSmartLookupValues(
        AppDbContext db,
        IEnumerable<SmartLookupEntry> entries,
        DateTime now)
    {
        var uniqueEntries = entries
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Value))
            .Select(entry => new
            {
                Category = entry.Category.Trim(),
                FieldType = entry.FieldType.Trim(),
                Value = entry.Value.Trim(),
                NormalizedValue = NormalizeLookupValue(entry.Value)
            })
            .GroupBy(entry => new
            {
                entry.Category,
                entry.FieldType,
                entry.NormalizedValue
            })
            .Select(group => group.First())
            .ToList();

        foreach (var entry in uniqueEntries)
        {
            SmartLookupValue? existing = db.SmartLookupValues
                .FirstOrDefault(item =>
                    item.Category == entry.Category &&
                    item.FieldType == entry.FieldType &&
                    item.NormalizedValue == entry.NormalizedValue);

            if (existing is null)
            {
                db.SmartLookupValues.Add(new SmartLookupValue
                {
                    Category = entry.Category,
                    FieldType = entry.FieldType,
                    Value = entry.Value,
                    NormalizedValue = entry.NormalizedValue,
                    UsageCount = 1,
                    LastUsedAt = now
                });
            }
            else
            {
                existing.Value = entry.Value;
                existing.UsageCount++;
                existing.LastUsedAt = now;
            }
        }
    }

    private static string NormalizeLookupValue(string value) =>
        string.Join(
            " ",
            value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .ToUpperInvariant();

    private static void Max(string? value, int length, string label)
    {
        if ((value?.Trim().Length ?? 0) > length)
            throw new InvalidOperationException($"{label}ยาวเกิน {length:N0} ตัวอักษร");
    }
}
