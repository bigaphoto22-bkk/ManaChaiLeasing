using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260828000100_AddPawnTicketDueDateOverride")]
public partial class AddPawnTicketDueDateOverride : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "DueDateOverride",
            table: "PawnTickets",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "DueDateOverrideRenewalCount",
            table: "PawnTickets",
            type: "INTEGER",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DueDateOverride",
            table: "PawnTickets");

        migrationBuilder.DropColumn(
            name: "DueDateOverrideRenewalCount",
            table: "PawnTickets");
    }
}
