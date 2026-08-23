using System.Globalization;
using ManaChaiLeasing.Data;
using ManaChaiLeasing.Models;
using Microsoft.EntityFrameworkCore;

namespace ManaChaiLeasing.Services;

public enum PawnTicketSearchFilter
{
    All = 0,
    Active = 1,
    DueToday = 2,
    Overdue = 3,
    Redeemed = 4,
    Sold = 5
}

public sealed class PawnTicketSearchResult
{
    public int Id { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public DateTime PawnDate { get; init; }

    public string PawnDateText => PawnDate.ToString("dd/MM/yyyy");

    public string CustomerName { get; init; } = string.Empty;

    public string? CitizenId { get; init; }

    public string? Phone { get; init; }

    public string ProductSummary { get; init; } = string.Empty;

    public decimal PrincipalAmount { get; init; }

    public string PrincipalAmountText => $"{PrincipalAmount:N2}";

    public PawnTicketStatus Status { get; init; }

    public int InterestRenewalCount { get; init; }

    public DateTime? CurrentDueDate { get; init; }

    public string CurrentDueDateText =>
        Status == PawnTicketStatus.Active &&
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

    public string DueStateText =>
        IsOverdue
            ? $"เกิน {(DateTime.Today - CurrentDueDate!.Value.Date).Days:N0} วัน"
            : IsDueToday
                ? "ครบกำหนดวันนี้"
                : string.Empty;

    public bool HasDueAlert =>
        IsDueToday || IsOverdue;

    public string StatusText => Status switch
    {
        PawnTicketStatus.Active => "กำลังจำนำ",
        PawnTicketStatus.Redeemed => "ไถ่ถอนแล้ว",
        PawnTicketStatus.Closed => "ปิดรายการ",
        PawnTicketStatus.Sold => "จำหน่ายแล้ว",
        _ => Status.ToString()
    };

    public string StatusDetailText => Status switch
    {
        PawnTicketStatus.Active when InterestRenewalCount > 0 =>
            $"ต่อดอกแล้ว {InterestRenewalCount:N0} ครั้ง",

        PawnTicketStatus.Active =>
            "จำนำใหม่",

        _ => string.Empty
    };

    public bool HasStatusDetail =>
        !string.IsNullOrWhiteSpace(StatusDetailText);

    public bool HasRenewalHistory =>
        Status == PawnTicketStatus.Active &&
        InterestRenewalCount > 0;
}

public sealed class PawnTicketDetail
{
    public int Id { get; init; }

    public string TicketNumber { get; init; } = string.Empty;

    public DateTime PawnDate { get; init; }

    public string PawnDateText => PawnDate.ToString("dd/MM/yyyy HH:mm");

    public decimal PrincipalAmount { get; init; }

    public string PrincipalAmountText => $"{PrincipalAmount:N2} บาท";

    public PawnTicketStatus Status { get; init; }

    public string StatusText => Status switch
    {
        PawnTicketStatus.Active => "กำลังจำนำ (Active)",
        PawnTicketStatus.Redeemed => "ไถ่ถอนแล้ว (Redeemed)",
        PawnTicketStatus.Closed => "ปิดรายการ (Closed)",
        PawnTicketStatus.Sold => "จำหน่ายแล้ว (Sold)",
        _ => Status.ToString()
    };

    public decimal InterestRatePercent { get; init; }

    public int InterestPeriodDays { get; init; }

    public int InterestRenewalCount { get; init; }

    public string InterestRateText =>
        $"{InterestRatePercent:0.##}%";

    public string InterestPeriodText =>
        $"{InterestPeriodDays:N0} วัน";

    public string InterestRenewalCountText =>
        $"{InterestRenewalCount:N0} ครั้ง";

    public string CurrentDueDateText =>
        PawnDate.Date
            .AddDays(
                InterestPeriodDays *
                (InterestRenewalCount + 1))
            .ToString("dd/MM/yyyy");

    public bool CanRenew =>
        Status == PawnTicketStatus.Active;

    public bool CanRedeem =>
        Status == PawnTicketStatus.Active;

    public bool CanSell =>
        Status == PawnTicketStatus.Active &&
        PawnDate.Date
            .AddDays(
                InterestPeriodDays *
                (InterestRenewalCount + 1)) <
            DateTime.Today;

    public string CustomerName { get; init; } = string.Empty;

    public string CitizenId { get; init; } = "-";

    public string AgeText { get; init; } = "-";

    public string Phone { get; init; } = "-";

    public string Address { get; init; } = "-";

    public string AssetCategory { get; init; } = "-";

    public string ProductType { get; init; } = "-";

    public string Brand { get; init; } = "-";

    public string Model { get; init; } = "-";

    public string CapacityOrSize { get; init; } = "-";

    public string Color { get; init; } = "-";

    public string ImeiOrSerial { get; init; } = "-";

    public string Accessories { get; init; } = "-";

    public string Condition { get; init; } = "-";

    public string Specification { get; init; } = "-";

    public string OtherDetails { get; init; } = "-";

    public string ProductSummary { get; init; } = "-";

    public string Note { get; init; } = "-";

    public List<PawnTransactionDetailRow> Transactions { get; init; } = new();

    public List<PawnTicketEditAuditRow> EditAudits { get; init; } = new();

    public bool HasEditAudits =>
        EditAudits.Count > 0;

    public string EditAuditCountText =>
        HasEditAudits
            ? $"ทั้งหมด {EditAudits.Count:N0} ครั้ง"
            : "ยังไม่มีการแก้ไขข้อมูล";
}

public sealed class PawnTicketEditAuditRow
{
    public DateTime EditedAt { get; init; }

    public string EditedAtText =>
        EditedAt.ToString("dd/MM/yyyy HH:mm");

    public string EditorUser { get; init; } = "-";

    public string EditorMachine { get; init; } = "-";

    public string EditorText =>
        $"{EditorUser} • เครื่อง {EditorMachine}";

    public string Reason { get; init; } = "-";

    public string ChangeSummary { get; init; } = "-";
}

public sealed class PawnTransactionDetailRow
{
    public DateTime TransactionDate { get; init; }

    public string TransactionDateText => TransactionDate.ToString("dd/MM/yyyy HH:mm");

    public PawnTransactionType TransactionType { get; init; }

    public string TransactionTypeText => TransactionType switch
    {
        PawnTransactionType.Pawn => "จำนำ",
        PawnTransactionType.Interest => "ต่อดอก",
        PawnTransactionType.Redemption => "ไถ่ถอน",
        PawnTransactionType.Sale => "จำหน่าย",
        _ => TransactionType.ToString()
    };

    public CashFlowType CashFlowType { get; init; }

    public string CashFlowText => CashFlowType switch
    {
        CashFlowType.Expense => "จ่ายออก",
        CashFlowType.Income => "รับเข้า",
        _ => CashFlowType.ToString()
    };

    public decimal Amount { get; init; }

    public string AmountText => $"{Amount:N2}";

    public int? InterestSequence { get; init; }

    public string InterestSequenceText =>
        InterestSequence.HasValue
            ? InterestSequence.Value.ToString()
            : "-";

    public string PaymentMethod { get; init; } = "-";

    public string Note { get; init; } = "-";
}

public sealed class PawnTicketSearchService
{
    public List<PawnTicketSearchResult> Search(
        string? keyword,
        PawnTicketSearchFilter filter = PawnTicketSearchFilter.All)
    {
        using AppDbContext db = new();

        IQueryable<PawnTicket> query = db.PawnTickets
            .AsNoTracking()
            .Include(ticket => ticket.Customer)
            .Include(ticket => ticket.Transactions);

        string term = keyword?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(term))
        {
            string pattern = $"%{term}%";

            bool hasAmount =
                TryParseAmount(term, out decimal amount);

            bool hasDate =
                DateTime.TryParse(
                    term,
                    CultureInfo.CurrentCulture,
                    DateTimeStyles.None,
                    out DateTime parsedDate);

            DateTime dateStart = parsedDate.Date;
            DateTime dateEnd = dateStart.AddDays(1);

            query = query.Where(ticket =>
                EF.Functions.Like(ticket.TicketNumber, pattern) ||
                EF.Functions.Like(ticket.Customer.FirstName, pattern) ||
                EF.Functions.Like(ticket.Customer.LastName, pattern) ||
                EF.Functions.Like(
                    ticket.Customer.FirstName + " " + ticket.Customer.LastName,
                    pattern) ||
                (ticket.Customer.CitizenId != null &&
                    EF.Functions.Like(ticket.Customer.CitizenId, pattern)) ||
                (ticket.Customer.Phone != null &&
                    EF.Functions.Like(ticket.Customer.Phone, pattern)) ||
                (ticket.Customer.Address != null &&
                    EF.Functions.Like(ticket.Customer.Address, pattern)) ||
                EF.Functions.Like(ticket.AssetCategory, pattern) ||
                (ticket.ProductType != null &&
                    EF.Functions.Like(ticket.ProductType, pattern)) ||
                (ticket.Brand != null &&
                    EF.Functions.Like(ticket.Brand, pattern)) ||
                (ticket.Model != null &&
                    EF.Functions.Like(ticket.Model, pattern)) ||
                (ticket.CapacityOrSize != null &&
                    EF.Functions.Like(ticket.CapacityOrSize, pattern)) ||
                (ticket.Color != null &&
                    EF.Functions.Like(ticket.Color, pattern)) ||
                (ticket.ImeiOrSerial != null &&
                    EF.Functions.Like(ticket.ImeiOrSerial, pattern)) ||
                (ticket.Accessories != null &&
                    EF.Functions.Like(ticket.Accessories, pattern)) ||
                (ticket.Condition != null &&
                    EF.Functions.Like(ticket.Condition, pattern)) ||
                (ticket.Specification != null &&
                    EF.Functions.Like(ticket.Specification, pattern)) ||
                (ticket.OtherDetails != null &&
                    EF.Functions.Like(ticket.OtherDetails, pattern)) ||
                EF.Functions.Like(ticket.ProductSummary, pattern) ||
                (ticket.Note != null &&
                    EF.Functions.Like(ticket.Note, pattern)) ||
                (hasAmount && ticket.PrincipalAmount == amount) ||
                (hasDate &&
                    ticket.PawnDate >= dateStart &&
                    ticket.PawnDate < dateEnd));
        }

        switch (filter)
        {
            case PawnTicketSearchFilter.Active:
            case PawnTicketSearchFilter.DueToday:
            case PawnTicketSearchFilter.Overdue:
                query = query.Where(ticket =>
                    ticket.Status == PawnTicketStatus.Active);
                break;

            case PawnTicketSearchFilter.Redeemed:
                query = query.Where(ticket =>
                    ticket.Status == PawnTicketStatus.Redeemed);
                break;

            case PawnTicketSearchFilter.Sold:
                query = query.Where(ticket =>
                    ticket.Status == PawnTicketStatus.Sold);
                break;
        }

        IOrderedQueryable<PawnTicket> orderedQuery = query
            .OrderByDescending(ticket => ticket.PawnDate)
            .ThenByDescending(ticket => ticket.Id);

        // Due/Overdue ต้องคำนวณวันครบกำหนดจากจำนวนครั้งต่อดอกจริง
        // จึงโหลด Active ที่ตรงคำค้นมาก่อนแล้วกรองวันที่ใน memory
        List<PawnTicket> tickets =
            filter is PawnTicketSearchFilter.DueToday
                or PawnTicketSearchFilter.Overdue
                ? orderedQuery.ToList()
                : orderedQuery.Take(300).ToList();

        IEnumerable<PawnTicketSearchResult> results =
            tickets.Select(BuildSearchResult);

        results = filter switch
        {
            PawnTicketSearchFilter.DueToday =>
                results.Where(item => item.IsDueToday),

            PawnTicketSearchFilter.Overdue =>
                results.Where(item => item.IsOverdue),

            _ => results
        };

        return results
            .Take(300)
            .ToList();
    }

    private static PawnTicketSearchResult BuildSearchResult(
        PawnTicket ticket)
    {
        int renewalCount = ticket.Transactions.Count(transaction =>
            !transaction.IsVoided &&
            transaction.TransactionType ==
                PawnTransactionType.Interest);

        DateTime? currentDueDate =
            ticket.Status == PawnTicketStatus.Active
                ? ticket.PawnDate.Date.AddDays(
                    ticket.InterestPeriodDays *
                    (renewalCount + 1))
                : null;

        return new PawnTicketSearchResult
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            PawnDate = ticket.PawnDate,
            CustomerName =
                $"{ticket.Customer.FirstName} {ticket.Customer.LastName}".Trim(),
            CitizenId = ticket.Customer.CitizenId,
            Phone = ticket.Customer.Phone,
            ProductSummary = ticket.ProductSummary,
            PrincipalAmount = ticket.PrincipalAmount,
            Status = ticket.Status,
            InterestRenewalCount = renewalCount,
            CurrentDueDate = currentDueDate
        };
    }

    public PawnTicketDetail GetDetail(int pawnTicketId)
    {
        using AppDbContext db = new();

        PawnTicket? ticket = db.PawnTickets
            .AsNoTracking()
            .Include(item => item.Customer)
            .Include(item => item.Transactions)
            .Include(item => item.EditAudits)
            .SingleOrDefault(item => item.Id == pawnTicketId);

        if (ticket is null)
        {
            throw new InvalidOperationException(
                "ไม่พบตั๋วจำนำที่เลือก");
        }

        return new PawnTicketDetail
        {
            Id = ticket.Id,
            TicketNumber = ticket.TicketNumber,
            PawnDate = ticket.PawnDate,
            PrincipalAmount = ticket.PrincipalAmount,
            Status = ticket.Status,
            InterestRatePercent = ticket.InterestRatePercent,
            InterestPeriodDays = ticket.InterestPeriodDays,
            InterestRenewalCount = ticket.Transactions.Count(transaction =>
                !transaction.IsVoided &&
                transaction.TransactionType == PawnTransactionType.Interest),
            CustomerName =
                $"{ticket.Customer.FirstName} {ticket.Customer.LastName}".Trim(),
            CitizenId = Display(ticket.Customer.CitizenId),
            AgeText = ticket.Customer.Age?.ToString() ?? "-",
            Phone = Display(ticket.Customer.Phone),
            Address = Display(ticket.Customer.Address),
            AssetCategory = Display(ticket.AssetCategory),
            ProductType = Display(ticket.ProductType),
            Brand = Display(ticket.Brand),
            Model = Display(ticket.Model),
            CapacityOrSize = Display(ticket.CapacityOrSize),
            Color = Display(ticket.Color),
            ImeiOrSerial = Display(ticket.ImeiOrSerial),
            Accessories = Display(ticket.Accessories),
            Condition = Display(ticket.Condition),
            Specification = Display(ticket.Specification),
            OtherDetails = Display(ticket.OtherDetails),
            ProductSummary = Display(ticket.ProductSummary),
            Note = Display(ticket.Note),
            Transactions = ticket.Transactions
                .Where(transaction => !transaction.IsVoided)
                .OrderBy(transaction => transaction.TransactionDate)
                .ThenBy(transaction => transaction.Id)
                .Select(transaction => new PawnTransactionDetailRow
                {
                    TransactionDate = transaction.TransactionDate,
                    TransactionType = transaction.TransactionType,
                    CashFlowType = transaction.CashFlowType,
                    Amount = transaction.Amount,
                    InterestSequence = transaction.InterestSequence,
                    PaymentMethod = Display(transaction.PaymentMethod),
                    Note = Display(transaction.Note)
                })
                .ToList(),
            EditAudits = ticket.EditAudits
                .OrderByDescending(audit => audit.EditedAt)
                .ThenByDescending(audit => audit.Id)
                .Select(audit => new PawnTicketEditAuditRow
                {
                    EditedAt = audit.EditedAt,
                    EditorUser = Display(audit.EditorUser),
                    EditorMachine = Display(audit.EditorMachine),
                    Reason = Display(audit.Reason),
                    ChangeSummary = Display(audit.ChangeSummary)
                })
                .ToList()
        };
    }

    private static bool TryParseAmount(
        string value,
        out decimal amount)
    {
        return decimal.TryParse(
                   value,
                   NumberStyles.Number,
                   CultureInfo.CurrentCulture,
                   out amount) ||
               decimal.TryParse(
                   value,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out amount);
    }

    private static string Display(string? value)
    {
        string text = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(text)
            ? "-"
            : text;
    }
}
