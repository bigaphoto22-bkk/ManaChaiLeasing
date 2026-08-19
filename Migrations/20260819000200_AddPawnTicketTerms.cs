using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819000200_AddPawnTicketTerms")]
public partial class AddPawnTicketTerms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "InterestPeriodDays",
            table: "PawnTickets",
            type: "INTEGER",
            nullable: false,
            defaultValue: 15);

        migrationBuilder.AddColumn<decimal>(
            name: "InterestRatePercent",
            table: "PawnTickets",
            type: "TEXT",
            precision: 8,
            scale: 2,
            nullable: false,
            defaultValue: 5m);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "InterestPeriodDays",
            table: "PawnTickets");

        migrationBuilder.DropColumn(
            name: "InterestRatePercent",
            table: "PawnTickets");
    }
}
