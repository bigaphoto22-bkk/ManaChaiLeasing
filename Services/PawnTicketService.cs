using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class PawnTicketSaveRequest
{
    public int? SelectedCustomerId { get; init; }

    public int? SourcePawnTicketId { get; init; }

    public Customer Customer { get; init; } = new();

    public PawnTicket Ticket { get; init; } = new();

    public IReadOnlyCollection<SmartLookupEntry> SmartLookupValues { get; init; }
        = Array.Empty<SmartLookupEntry>();
}

public sealed record SmartLookupEntry(
    string Category,
    string FieldType,
    string Value);

public sealed class PawnTicketService
{
    public PawnTicket SavePawnTicket(PawnTicketSaveRequest request)
    {
        lock (BusinessTransactionGate.SyncRoot)
        {
            using AppDbContext db = new();
            using var dbTransaction = db.Database.BeginTransaction();

            string ticketNumber = request.Ticket.TicketNumber.Trim();

            bool ticketExists = db.PawnTickets.Any(item =>
                item.TicketNumber.ToUpper() == ticketNumber.ToUpper());

            if (ticketExists)
            {
                throw new InvalidOperationException(
                    $"หมายเลขตั๋ว {ticketNumber} มีอยู่ในระบบแล้ว");
            }

            DateTime now = DateTime.Now;

            PawnTicket? repawnSource =
                ValidateRepawnSource(
                    db,
                    request.SourcePawnTicketId,
                    request.Ticket);

            Customer customer = ResolveCustomer(
                db,
                request.Customer,
                request.SelectedCustomerId,
                now);

            if (repawnSource is not null &&
                customer.Id != repawnSource.CustomerId)
            {
                throw new InvalidOperationException(
                    "ลูกค้าในตั๋วใหม่ไม่ตรงกับเจ้าของตั๋วเดิม");
            }

            AppSetting settings = db.AppSettings
                .AsNoTracking()
                .OrderBy(item => item.Id)
                .FirstOrDefault()
                ?? throw new InvalidOperationException(
                    "ไม่พบการตั้งค่าดอกเบี้ย กรุณาเข้าเมนูตั้งค่าแล้วบันทึกข้อมูลก่อน");

            PawnTicket ticket = request.Ticket;

            ticket.TicketNumber = ticketNumber;
            // DatePicker provides a date-only value at 00:00. Keep the
            // selected business date, but record the actual save time so
            // Today's Transactions does not display every pawn as 00:00.
            ticket.PawnDate =
                ticket.PawnDate.Date.Add(now.TimeOfDay);
            ticket.Customer = customer;
            ticket.InterestRatePercent = settings.InterestRatePercent;
            ticket.InterestPeriodDays = settings.InterestPeriodDays;
            ticket.Status = PawnTicketStatus.Active;
            ticket.SourcePawnTicketId =
                repawnSource?.Id;
            ticket.CreatedAt = now;
            ticket.UpdatedAt = now;

            ticket.Transactions.Add(new PawnTransaction
            {
                TransactionType = PawnTransactionType.Pawn,
                CashFlowType = CashFlowType.Expense,
                TransactionDate = ticket.PawnDate,
                Amount = ticket.PrincipalAmount,
                CreatedAt = now
            });

            db.PawnTickets.Add(ticket);

            LearnSmartLookupValues(
                db,
                request.SmartLookupValues,
                now);

            db.SaveChanges();
            dbTransaction.Commit();

            return ticket;
    
        }
}

    public List<string> GetSmartLookupValues(
        string category,
        string fieldType)
    {
        using AppDbContext db = new();

        return db.SmartLookupValues
            .AsNoTracking()
            .Where(item =>
                item.Category == category &&
                item.FieldType == fieldType)
            .OrderByDescending(item => item.UsageCount)
            .ThenByDescending(item => item.LastUsedAt)
            .ThenBy(item => item.Value)
            .Select(item => item.Value)
            .ToList();
    }

    private static Customer ResolveCustomer(
        AppDbContext db,
        Customer input,
        int? selectedCustomerId,
        DateTime now)
    {
        Customer? customer = null;

        if (selectedCustomerId.HasValue)
        {
            customer = db.Customers
                .FirstOrDefault(item =>
                    item.Id == selectedCustomerId.Value);

            if (customer is null)
            {
                throw new InvalidOperationException(
                    "ไม่พบข้อมูลลูกค้าที่เลือกไว้ กรุณาค้นหาลูกค้าใหม่อีกครั้ง");
            }
        }

        string? citizenId = CleanOptional(input.CitizenId);

        if (!string.IsNullOrWhiteSpace(citizenId))
        {
            Customer? duplicateCitizenIdCustomer = db.Customers
                .AsNoTracking()
                .FirstOrDefault(item =>
                    item.CitizenId == citizenId &&
                    (!selectedCustomerId.HasValue ||
                     item.Id != selectedCustomerId.Value));

            if (duplicateCitizenIdCustomer is not null)
            {
                throw new InvalidOperationException(
                    "เลขบัตรประชาชนนี้มีข้อมูลลูกค้าอยู่แล้ว กรุณาใช้ปุ่มค้นหาลูกค้าเก่า");
            }
        }

        if (customer is null)
        {
            customer = new Customer
            {
                CreatedAt = now
            };

            db.Customers.Add(customer);
        }

        customer.FirstName = input.FirstName.Trim();
        customer.LastName = input.LastName.Trim();
        customer.CitizenId = citizenId;
        customer.Age = input.Age;
        customer.Phone = CleanOptional(input.Phone);
        customer.Address = CleanOptional(input.Address);
        customer.UpdatedAt = now;

        return customer;
    }

    private static PawnTicket? ValidateRepawnSource(
        AppDbContext db,
        int? sourcePawnTicketId,
        PawnTicket newTicket)
    {
        if (!sourcePawnTicketId.HasValue)
        {
            return null;
        }

        PawnTicket? source = db.PawnTickets
            .SingleOrDefault(ticket =>
                ticket.Id == sourcePawnTicketId.Value);

        if (source is null)
        {
            throw new InvalidOperationException(
                "ไม่พบตั๋วเดิมที่ใช้สร้างรายการจำนำใหม่");
        }

        if (source.Status != PawnTicketStatus.Redeemed)
        {
            throw new InvalidOperationException(
                "ตั๋วเดิมต้องอยู่ในสถานะไถ่ถอนแล้วเท่านั้น");
        }

        PawnTicket? existingRepawn = db.PawnTickets
            .AsNoTracking()
            .FirstOrDefault(ticket =>
                ticket.SourcePawnTicketId == source.Id);

        if (existingRepawn is not null)
        {
            throw new InvalidOperationException(
                $"ตั๋วเดิมถูกนำไปสร้างตั๋วใหม่ " +
                $"{existingRepawn.TicketNumber} แล้ว");
        }

        string normalizedSerial =
            RepawnService.NormalizeSerial(
                newTicket.ImeiOrSerial);

        if (!string.IsNullOrWhiteSpace(normalizedSerial))
        {
            PawnTicket? activeDuplicate = db.PawnTickets
                .AsNoTracking()
                .Where(ticket =>
                    ticket.Status == PawnTicketStatus.Active &&
                    ticket.ImeiOrSerial != null)
                .AsEnumerable()
                .FirstOrDefault(ticket =>
                    RepawnService.NormalizeSerial(
                        ticket.ImeiOrSerial) ==
                    normalizedSerial);

            if (activeDuplicate is not null)
            {
                throw new InvalidOperationException(
                    $"IMEI / Serial นี้ยังอยู่ในตั๋วที่กำลังจำนำ " +
                    $"{activeDuplicate.TicketNumber}");
            }
        }

        return source;
    }

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
                Value = CleanRequired(entry.Value),
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
                existing.UsageCount += 1;
                existing.LastUsedAt = now;
            }
        }
    }

    private static string NormalizeLookupValue(string value)
    {
        string collapsed = string.Join(
            " ",
            value.Trim()
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries));

        return collapsed.ToUpperInvariant();
    }

    private static string CleanRequired(string value)
    {
        return value.Trim();
    }

    private static string? CleanOptional(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(cleaned)
            ? null
            : cleaned;
    }
}
