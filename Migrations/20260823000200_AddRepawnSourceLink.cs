using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260823000200_AddRepawnSourceLink")]
public partial class AddRepawnSourceLink : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "SourcePawnTicketId",
            table: "PawnTickets",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PawnTickets_SourcePawnTicketId",
            table: "PawnTickets",
            column: "SourcePawnTicketId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_PawnTickets_SourcePawnTicketId",
            table: "PawnTickets");

        migrationBuilder.DropColumn(
            name: "SourcePawnTicketId",
            table: "PawnTickets");
    }
}
