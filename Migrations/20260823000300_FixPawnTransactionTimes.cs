using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260823000300_FixPawnTransactionTimes")]
public partial class FixPawnTransactionTimes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Older pawn tickets were created from a DatePicker value, so their
        // PawnDate and first transaction were stored at 00:00. CreatedAt
        // already contains the real save time. Preserve the selected date
        // and restore only its time component.
        migrationBuilder.Sql(
            """
            UPDATE PawnTickets
            SET PawnDate =
                substr(PawnDate, 1, 10) ||
                substr(CreatedAt, 11)
            WHERE substr(PawnDate, 12, 8) = '00:00:00'
              AND length(CreatedAt) >= 19;
            """);

        migrationBuilder.Sql(
            """
            UPDATE PawnTransactions
            SET TransactionDate =
                substr(TransactionDate, 1, 10) ||
                substr(CreatedAt, 11)
            WHERE TransactionType = 'Pawn'
              AND substr(TransactionDate, 12, 8) = '00:00:00'
              AND length(CreatedAt) >= 19;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Data repair only. Restoring 00:00 would discard valid time data,
        // so the corrected values are intentionally retained on rollback.
    }
}
