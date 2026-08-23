using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class PawnTicketEditData
{
    public int PawnTicketId { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public string LockedTicketSummary { get; init; } = string.Empty;

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

    public string Note { get; init; } = string.Empty;
}

public sealed class PawnTicketEditRequest
{
    public int PawnTicketId { get; init; }

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

    public string? Note { get; init; }

    public string Reason { get; init; } = string.Empty;
}

public sealed record PawnTicketEditResult(
    int PawnTicketId,
    int ChangedFieldCount,
    DateTime EditedAt);

public sealed class PawnTicketEditService
{
    private sealed record FieldChange(
        string Label,
        string OldValue,
        string NewValue);

    public PawnTicketEditData GetEditData(
        int pawnTicketId)
    {
        using AppDbContext db = new();

        PawnTicket? ticket = db.PawnTickets
            .AsNoTracking()
            .Include(item => item.Customer)
            .SingleOrDefault(item =>
                item.Id == pawnTicketId);

        if (ticket is null)
        {
            throw new InvalidOperationException(
                "ไม่พบตั๋วจำนำที่ต้องการแก้ไข");
        }

        return new PawnTicketEditData
        {
            PawnTicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            LockedTicketSummary =
                $"วันที่ {ticket.PawnDate:dd/MM/yyyy} • " +
                $"เงินต้น {ticket.PrincipalAmount:N2} บาท • " +
                $"ดอกเบี้ย {ticket.InterestRatePercent:0.##}% / " +
                $"{ticket.InterestPeriodDays:N0} วัน • " +
                $"สถานะ {StatusText(ticket.Status)}",
            FirstName = ticket.Customer.FirstName,
            LastName = ticket.Customer.LastName,
            CitizenId = ticket.Customer.CitizenId ?? string.Empty,
            Age = ticket.Customer.Age,
            Phone = ticket.Customer.Phone ?? string.Empty,
            Address = ticket.Customer.Address ?? string.Empty,
            AssetCategory = ticket.AssetCategory,
            ProductType = ticket.ProductType ?? string.Empty,
            Brand = ticket.Brand ?? string.Empty,
            Model = ticket.Model ?? string.Empty,
            CapacityOrSize = ticket.CapacityOrSize ?? string.Empty,
            Color = ticket.Color ?? string.Empty,
            ImeiOrSerial = ticket.ImeiOrSerial ?? string.Empty,
            Accessories = ticket.Accessories ?? string.Empty,
            Condition = ticket.Condition ?? string.Empty,
            Specification = ticket.Specification ?? string.Empty,
            OtherDetails = ticket.OtherDetails ?? string.Empty,
            ProductSummary = ticket.ProductSummary,
            Note = ticket.Note ?? string.Empty
        };
    }

    public PawnTicketEditResult Save(
        PawnTicketEditRequest request)
    {
        lock (BusinessTransactionGate.SyncRoot)
        {
            using AppDbContext db = new();
            using var dbTransaction =
                db.Database.BeginTransaction();

            PawnTicket? ticket = db.PawnTickets
                .Include(item => item.Customer)
                .SingleOrDefault(item =>
                    item.Id == request.PawnTicketId);

            if (ticket is null)
            {
                throw new InvalidOperationException(
                    "ไม่พบตั๋วจำนำที่ต้องการแก้ไข");
            }

            string firstName =
                CleanRequired(
                    request.FirstName,
                    "ชื่อ");

            string lastName =
                CleanRequired(
                    request.LastName,
                    "นามสกุล");

            string assetCategory =
                CleanRequired(
                    request.AssetCategory,
                    "ประเภทหลัก");

            string productSummary =
                CleanRequired(
                    request.ProductSummary,
                    "รายละเอียดสินค้า");

            string reason =
                CleanOptional(request.Reason)
                ?? "ไม่ได้ระบุ";

            string? citizenId =
                CleanOptional(request.CitizenId);

            ValidateCitizenId(citizenId);
            ValidateAge(request.Age);

            EnsureMaxLength(firstName, 100, "ชื่อ");
            EnsureMaxLength(lastName, 100, "นามสกุล");
            EnsureMaxLength(citizenId, 13, "เลขบัตรประชาชน");
            EnsureMaxLength(request.Phone, 30, "โทรศัพท์");
            EnsureMaxLength(request.Address, 1000, "ที่อยู่");
            EnsureMaxLength(assetCategory, 100, "ประเภทหลัก");
            EnsureMaxLength(request.ProductType, 100, "ประเภทสินค้า");
            EnsureMaxLength(request.Brand, 100, "ยี่ห้อ");
            EnsureMaxLength(request.Model, 200, "รุ่น");
            EnsureMaxLength(request.CapacityOrSize, 100, "ความจุ / ขนาด");
            EnsureMaxLength(request.Color, 100, "สี");
            EnsureMaxLength(request.ImeiOrSerial, 150, "IMEI / Serial");
            EnsureMaxLength(request.Accessories, 500, "อุปกรณ์");
            EnsureMaxLength(request.Condition, 1000, "สภาพ / ตำหนิ");
            EnsureMaxLength(request.Specification, 1500, "สเปก");
            EnsureMaxLength(request.OtherDetails, 1500, "รายละเอียดอื่น ๆ");
            EnsureMaxLength(productSummary, 2500, "รายละเอียดสินค้า");
            EnsureMaxLength(request.Note, 1500, "หมายเหตุตั๋ว");
            EnsureMaxLength(reason, 1000, "เหตุผลการแก้ไข");

            if (!string.IsNullOrWhiteSpace(citizenId))
            {
                bool duplicateCitizenId = db.Customers
                    .AsNoTracking()
                    .Any(customer =>
                        customer.Id != ticket.CustomerId &&
                        customer.CitizenId == citizenId);

                if (duplicateCitizenId)
                {
                    throw new InvalidOperationException(
                        "เลขบัตรประชาชนนี้ถูกใช้กับลูกค้าคนอื่นแล้ว");
                }
            }

            string? phone = CleanOptional(request.Phone);
            string? address = CleanOptional(request.Address);
            string? productType = CleanOptional(request.ProductType);
            string? brand = CleanOptional(request.Brand);
            string? model = CleanOptional(request.Model);
            string? capacityOrSize = CleanOptional(request.CapacityOrSize);
            string? color = CleanOptional(request.Color);
            string? imeiOrSerial = CleanOptional(request.ImeiOrSerial);
            string? accessories = CleanOptional(request.Accessories);
            string? condition = CleanOptional(request.Condition);
            string? specification = CleanOptional(request.Specification);
            string? otherDetails = CleanOptional(request.OtherDetails);
            string? note = CleanOptional(request.Note);

            List<FieldChange> changes = [];

            TrackChange(changes, "ชื่อลูกค้า", ticket.Customer.FirstName, firstName);
            TrackChange(changes, "นามสกุลลูกค้า", ticket.Customer.LastName, lastName);
            TrackChange(changes, "เลขบัตรประชาชน", ticket.Customer.CitizenId, citizenId);
            TrackChange(changes, "อายุ", ticket.Customer.Age?.ToString(), request.Age?.ToString());
            TrackChange(changes, "โทรศัพท์", ticket.Customer.Phone, phone);
            TrackChange(changes, "ที่อยู่", ticket.Customer.Address, address);

            int customerChangeCount =
                changes.Count;

            TrackChange(changes, "ประเภทหลัก", ticket.AssetCategory, assetCategory);
            TrackChange(changes, "ประเภทสินค้า", ticket.ProductType, productType);
            TrackChange(changes, "ยี่ห้อ", ticket.Brand, brand);
            TrackChange(changes, "รุ่น", ticket.Model, model);
            TrackChange(changes, "ความจุ / ขนาด", ticket.CapacityOrSize, capacityOrSize);
            TrackChange(changes, "สี", ticket.Color, color);
            TrackChange(changes, "IMEI / Serial", ticket.ImeiOrSerial, imeiOrSerial);
            TrackChange(changes, "อุปกรณ์", ticket.Accessories, accessories);
            TrackChange(changes, "สภาพ / ตำหนิ", ticket.Condition, condition);
            TrackChange(changes, "สเปก", ticket.Specification, specification);
            TrackChange(changes, "รายละเอียดอื่น ๆ", ticket.OtherDetails, otherDetails);
            TrackChange(changes, "รายละเอียดสินค้า", ticket.ProductSummary, productSummary);
            TrackChange(changes, "หมายเหตุตั๋ว", ticket.Note, note);

            if (changes.Count == 0)
            {
                throw new InvalidOperationException(
                    "ไม่มีข้อมูลเปลี่ยนแปลง กรุณาตรวจสอบอีกครั้ง");
            }

            DateTime now = DateTime.Now;

            if (customerChangeCount > 0)
            {
                ticket.Customer.FirstName = firstName;
                ticket.Customer.LastName = lastName;
                ticket.Customer.CitizenId = citizenId;
                ticket.Customer.Age = request.Age;
                ticket.Customer.Phone = phone;
                ticket.Customer.Address = address;
                ticket.Customer.UpdatedAt = now;
            }

            ticket.AssetCategory = assetCategory;
            ticket.ProductType = productType;
            ticket.Brand = brand;
            ticket.Model = model;
            ticket.CapacityOrSize = capacityOrSize;
            ticket.Color = color;
            ticket.ImeiOrSerial = imeiOrSerial;
            ticket.Accessories = accessories;
            ticket.Condition = condition;
            ticket.Specification = specification;
            ticket.OtherDetails = otherDetails;
            ticket.ProductSummary = productSummary;
            ticket.Note = note;
            ticket.UpdatedAt = now;

            db.PawnTicketEditAudits.Add(
                new PawnTicketEditAudit
                {
                    PawnTicketId = ticket.Id,
                    EditedAt = now,
                    EditorUser = CurrentWindowsUser(),
                    EditorMachine = Environment.MachineName,
                    Reason = reason,
                    ChangeSummary = string.Join(
                        Environment.NewLine,
                        changes.Select(change =>
                            $"{change.Label}: " +
                            $"{AuditValue(change.OldValue)} → " +
                            $"{AuditValue(change.NewValue)}"))
                });

            db.SaveChanges();
            dbTransaction.Commit();

            return new PawnTicketEditResult(
                ticket.Id,
                changes.Count,
                now);
        }
    }

    private static void TrackChange(
        ICollection<FieldChange> changes,
        string label,
        string? oldValue,
        string? newValue)
    {
        string oldClean = oldValue?.Trim() ?? string.Empty;
        string newClean = newValue?.Trim() ?? string.Empty;

        if (!string.Equals(
                oldClean,
                newClean,
                StringComparison.Ordinal))
        {
            changes.Add(
                new FieldChange(
                    label,
                    oldClean,
                    newClean));
        }
    }

    private static string CleanRequired(
        string? value,
        string label)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            throw new InvalidOperationException(
                $"กรุณากรอก{label}");
        }

        return cleaned;
    }

    private static string? CleanOptional(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(cleaned)
            ? null
            : cleaned;
    }

    private static void ValidateCitizenId(string? citizenId)
    {
        if (!string.IsNullOrWhiteSpace(citizenId) &&
            (citizenId.Length != 13 ||
             !citizenId.All(char.IsDigit)))
        {
            throw new InvalidOperationException(
                "เลขบัตรประชาชนต้องเป็นตัวเลข 13 หลัก หรือเว้นว่างไว้");
        }
    }

    private static void ValidateAge(int? age)
    {
        if (age.HasValue &&
            (age.Value < 1 || age.Value > 120))
        {
            throw new InvalidOperationException(
                "อายุต้องอยู่ระหว่าง 1 - 120 ปี หรือเว้นว่างไว้");
        }
    }

    private static void EnsureMaxLength(
        string? value,
        int maximumLength,
        string label)
    {
        if ((value?.Trim().Length ?? 0) > maximumLength)
        {
            throw new InvalidOperationException(
                $"{label}ยาวเกิน {maximumLength:N0} ตัวอักษร");
        }
    }

    private static string AuditValue(string value)
    {
        string cleaned = value
            .Replace("\r\n", " ⏎ ")
            .Replace("\n", " ⏎ ")
            .Trim();

        return string.IsNullOrWhiteSpace(cleaned)
            ? "(ว่าง)"
            : $"“{cleaned}”";
    }

    private static string CurrentWindowsUser()
    {
        string domain = Environment.UserDomainName.Trim();
        string user = Environment.UserName.Trim();

        return string.IsNullOrWhiteSpace(domain)
            ? user
            : $"{domain}\\{user}";
    }

    private static string StatusText(
        PawnTicketStatus status) => status switch
        {
            PawnTicketStatus.Active => "กำลังจำนำ",
            PawnTicketStatus.Redeemed => "ไถ่ถอนแล้ว",
            PawnTicketStatus.Sold => "จำหน่ายแล้ว",
            PawnTicketStatus.Closed => "ปิดรายการ",
            _ => status.ToString()
        };
}
