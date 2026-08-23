using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class RepawnDraft
{
    public int SourcePawnTicketId { get; init; }

    public string SourceTicketNumber { get; init; } = string.Empty;

    public int CustomerId { get; init; }

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
}

public sealed class RepawnService
{
    public RepawnDraft CreateDraft(
        int sourcePawnTicketId)
    {
        using AppDbContext db = new();

        PawnTicket? source = db.PawnTickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .SingleOrDefault(ticket =>
                ticket.Id == sourcePawnTicketId);

        if (source is null)
        {
            throw new InvalidOperationException(
                "ไม่พบตั๋วเดิมที่ต้องการนำกลับมาจำนำใหม่");
        }

        if (source.Status != PawnTicketStatus.Redeemed)
        {
            throw new InvalidOperationException(
                "จำนำสินค้าเดิมอีกครั้งได้เฉพาะตั๋วที่ไถ่ถอนแล้วเท่านั้น");
        }

        PawnTicket? existingRepawn = db.PawnTickets
            .AsNoTracking()
            .FirstOrDefault(ticket =>
                ticket.SourcePawnTicketId == source.Id);

        if (existingRepawn is not null)
        {
            throw new InvalidOperationException(
                $"ตั๋ว {source.TicketNumber} ถูกนำไปสร้างตั๋วใหม่ " +
                $"{existingRepawn.TicketNumber} แล้ว\n\n" +
                "หากสินค้าถูกไถ่ถอนอีกครั้ง ให้เริ่มจากตั๋วล่าสุด");
        }

        EnsureNoActiveSerialDuplicate(
            db,
            source.ImeiOrSerial,
            source.Id);

        return new RepawnDraft
        {
            SourcePawnTicketId = source.Id,
            SourceTicketNumber = source.TicketNumber,
            CustomerId = source.CustomerId,
            FirstName = source.Customer.FirstName,
            LastName = source.Customer.LastName,
            CitizenId = source.Customer.CitizenId ?? string.Empty,
            Age = source.Customer.Age,
            Phone = source.Customer.Phone ?? string.Empty,
            Address = source.Customer.Address ?? string.Empty,
            AssetCategory = source.AssetCategory,
            ProductType = source.ProductType ?? string.Empty,
            Brand = source.Brand ?? string.Empty,
            Model = source.Model ?? string.Empty,
            CapacityOrSize = source.CapacityOrSize ?? string.Empty,
            Color = source.Color ?? string.Empty,
            ImeiOrSerial = source.ImeiOrSerial ?? string.Empty,
            Accessories = source.Accessories ?? string.Empty,
            Condition = source.Condition ?? string.Empty,
            Specification = source.Specification ?? string.Empty,
            OtherDetails = source.OtherDetails ?? string.Empty
        };
    }

    private static void EnsureNoActiveSerialDuplicate(
        AppDbContext db,
        string? imeiOrSerial,
        int sourcePawnTicketId)
    {
        string normalized =
            NormalizeSerial(imeiOrSerial);

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        PawnTicket? activeDuplicate = db.PawnTickets
            .AsNoTracking()
            .Where(ticket =>
                ticket.Id != sourcePawnTicketId &&
                ticket.Status == PawnTicketStatus.Active &&
                ticket.ImeiOrSerial != null)
            .AsEnumerable()
            .FirstOrDefault(ticket =>
                NormalizeSerial(ticket.ImeiOrSerial) ==
                normalized);

        if (activeDuplicate is not null)
        {
            throw new InvalidOperationException(
                $"IMEI / Serial นี้ยังอยู่ในตั๋วที่กำลังจำนำ " +
                $"{activeDuplicate.TicketNumber}\n\n" +
                "ระบบไม่อนุญาตให้สร้างตั๋วซ้ำสำหรับสินค้าชิ้นเดียวกัน");
        }
    }

    internal static string NormalizeSerial(
        string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        return new string(
            cleaned
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray());
    }
}
