using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public sealed class ThaiIdCustomerHistorySummary
{
    public int CustomerId { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string CitizenId { get; init; } = string.Empty;

    public string Phone { get; init; } = "-";

    public DateTime? LastServiceDate { get; init; }

    public int TotalTicketCount { get; init; }

    public int ActiveCount { get; init; }

    public int DueTodayCount { get; init; }

    public int OverdueCount { get; init; }

    public int RedeemedCount { get; init; }

    public int SoldCount { get; init; }

    public List<ThaiIdCustomerHistoryRow> Tickets { get; init; } = [];

    public string CitizenIdText =>
        FormatCitizenId(CitizenId);

    public string LastServiceDateText =>
        LastServiceDate.HasValue
            ? LastServiceDate.Value.ToString("dd/MM/yyyy HH:mm")
            : "ยังไม่มีประวัติตั๋ว";

    public string AlertText =>
        OverdueCount > 0
            ? $"พบตั๋วเกินกำหนด {OverdueCount:N0} ตั๋ว • กรุณาตรวจสอบก่อนทำรายการใหม่"
            : DueTodayCount > 0
                ? $"มีตั๋วครบกำหนดวันนี้ {DueTodayCount:N0} ตั๋ว"
                : ActiveCount > 0
                    ? $"มีตั๋วกำลังจำนำ {ActiveCount:N0} ตั๋ว"
                    : TotalTicketCount > 0
                        ? "ไม่พบตั๋วค้าง • มีเฉพาะประวัติที่ปิดรายการแล้ว"
                        : "พบข้อมูลลูกค้าเดิม แต่ยังไม่มีประวัติตั๋วจำนำ";

    public ThaiIdCustomerHistoryAlertLevel AlertLevel =>
        OverdueCount > 0
            ? ThaiIdCustomerHistoryAlertLevel.Overdue
            : DueTodayCount > 0
                ? ThaiIdCustomerHistoryAlertLevel.DueToday
                : ActiveCount > 0
                    ? ThaiIdCustomerHistoryAlertLevel.Active
                    : ThaiIdCustomerHistoryAlertLevel.Clear;

    private static string FormatCitizenId(string value)
    {
        string digits =
            new(value.Where(char.IsDigit).ToArray());

        return digits.Length == 13
            ? $"{digits[0]}-{digits.Substring(1, 4)}-{digits.Substring(5, 5)}-{digits.Substring(10, 2)}-{digits[12]}"
            : value;
    }
}

public enum ThaiIdCustomerHistoryAlertLevel
{
    Clear,
    Active,
    DueToday,
    Overdue
}

public sealed class ThaiIdCustomerHistoryRow
{
    public int PawnTicketId { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public DateTime PawnDate { get; init; }

    public DateTime LastActivityDate { get; init; }

    public string ProductSummary { get; init; } = string.Empty;

    public decimal PrincipalAmount { get; init; }

    public PawnTicketStatus Status { get; init; }

    public DateTime? CurrentDueDate { get; init; }

    public bool HasRepawnTicket { get; init; }

    public bool CanRepawn =>
        Status == PawnTicketStatus.Redeemed &&
        !HasRepawnTicket;

    public string PawnDateText =>
        PawnDate.ToString("dd/MM/yyyy");

    public string LastActivityDateText =>
        LastActivityDate.ToString("dd/MM/yyyy HH:mm");

    public string PrincipalAmountText =>
        $"{PrincipalAmount:N2}";

    public string CurrentDueDateText =>
        CurrentDueDate.HasValue
            ? CurrentDueDate.Value.ToString("dd/MM/yyyy")
            : "-";

    public bool IsDueToday =>
        Status == PawnTicketStatus.Active &&
        CurrentDueDate.HasValue &&
        CurrentDueDate.Value.Date == DateTime.Today;

    public bool IsOverdue =>
        Status == PawnTicketStatus.Active &&
        CurrentDueDate.HasValue &&
        CurrentDueDate.Value.Date < DateTime.Today;

    public string StatusText => Status switch
    {
        PawnTicketStatus.Active when IsOverdue => "เกินกำหนด",
        PawnTicketStatus.Active when IsDueToday => "ครบกำหนดวันนี้",
        PawnTicketStatus.Active => "กำลังจำนำ",
        PawnTicketStatus.Redeemed when HasRepawnTicket =>
            "ไถ่ถอนแล้ว • จำนำใหม่แล้ว",
        PawnTicketStatus.Redeemed => "ไถ่ถอนแล้ว",
        PawnTicketStatus.Sold => "จำหน่ายแล้ว",
        PawnTicketStatus.Closed => "ปิดรายการ",
        _ => Status.ToString()
    };
}

public sealed class ThaiIdCustomerHistoryService
{
    public ThaiIdCustomerHistorySummary GetHistory(int customerId)
    {
        using AppDbContext db = new();

        Customer? customer = db.Customers
            .AsNoTracking()
            .Include(item => item.PawnTickets)
                .ThenInclude(ticket => ticket.Transactions)
            .SingleOrDefault(item => item.Id == customerId);

        if (customer is null)
        {
            throw new InvalidOperationException(
                "ไม่พบข้อมูลลูกค้าที่ต้องการดูประวัติ");
        }

        HashSet<int> repawnedSourceIds = customer.PawnTickets
            .Where(ticket =>
                ticket.SourcePawnTicketId.HasValue)
            .Select(ticket =>
                ticket.SourcePawnTicketId!.Value)
            .ToHashSet();

        List<ThaiIdCustomerHistoryRow> rows = customer.PawnTickets
            .Select(ticket =>
                BuildRow(
                    ticket,
                    repawnedSourceIds.Contains(ticket.Id)))
            .OrderByDescending(item => item.LastActivityDate)
            .ThenByDescending(item => item.PawnTicketId)
            .ToList();

        DateTime? lastServiceDate = rows.Count > 0
            ? rows.Max(item => item.LastActivityDate)
            : null;

        return new ThaiIdCustomerHistorySummary
        {
            CustomerId = customer.Id,
            CustomerName =
                $"{customer.FirstName} {customer.LastName}".Trim(),
            CitizenId = customer.CitizenId ?? string.Empty,
            Phone = Display(customer.Phone),
            LastServiceDate = lastServiceDate,
            TotalTicketCount = rows.Count,
            ActiveCount = rows.Count(item =>
                item.Status == PawnTicketStatus.Active),
            DueTodayCount = rows.Count(item =>
                item.IsDueToday),
            OverdueCount = rows.Count(item =>
                item.IsOverdue),
            RedeemedCount = rows.Count(item =>
                item.Status == PawnTicketStatus.Redeemed),
            SoldCount = rows.Count(item =>
                item.Status == PawnTicketStatus.Sold),
            Tickets = rows
        };
    }

    private static ThaiIdCustomerHistoryRow BuildRow(
        PawnTicket ticket,
        bool hasRepawnTicket)
    {
        List<PawnTransaction> activeTransactions = ticket.Transactions
            .Where(transaction => !transaction.IsVoided)
            .ToList();

        int renewalCount = activeTransactions.Count(transaction =>
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        DateTime? currentDueDate =
            ticket.Status == PawnTicketStatus.Active
                ? ticket.PawnDate.Date.AddDays(
                    ticket.InterestPeriodDays *
                    (renewalCount + 1))
                : null;

        DateTime lastActivityDate = activeTransactions
            .Select(transaction => transaction.TransactionDate)
            .DefaultIfEmpty(ticket.PawnDate)
            .Max();

        return new ThaiIdCustomerHistoryRow
        {
            PawnTicketId = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            PawnDate = ticket.PawnDate,
            LastActivityDate = lastActivityDate,
            ProductSummary = ticket.ProductSummary,
            PrincipalAmount = ticket.PrincipalAmount,
            Status = ticket.Status,
            CurrentDueDate = currentDueDate,
            HasRepawnTicket = hasRepawnTicket
        };
    }

    private static string Display(string? value)
    {
        string cleaned = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(cleaned)
            ? "-"
            : cleaned;
    }
}
