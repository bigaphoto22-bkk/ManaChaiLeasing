using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260823000100_AddPawnTicketEditAudits")]
public partial class AddPawnTicketEditAudits : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PawnTicketEditAudits",
            columns: table => new
            {
                Id = table.Column<int>(
                        type: "INTEGER",
                        nullable: false)
                    .Annotation(
                        "Sqlite:Autoincrement",
                        true),
                PawnTicketId = table.Column<int>(
                    type: "INTEGER",
                    nullable: false),
                EditedAt = table.Column<DateTime>(
                    type: "TEXT",
                    nullable: false),
                EditorUser = table.Column<string>(
                    type: "TEXT",
                    maxLength: 200,
                    nullable: false),
                EditorMachine = table.Column<string>(
                    type: "TEXT",
                    maxLength: 100,
                    nullable: false),
                Reason = table.Column<string>(
                    type: "TEXT",
                    maxLength: 1000,
                    nullable: false),
                ChangeSummary = table.Column<string>(
                    type: "TEXT",
                    maxLength: 12000,
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_PawnTicketEditAudits",
                    x => x.Id);

                table.ForeignKey(
                    name: "FK_PawnTicketEditAudits_PawnTickets_PawnTicketId",
                    column: x => x.PawnTicketId,
                    principalTable: "PawnTickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PawnTicketEditAudits_PawnTicketId",
            table: "PawnTicketEditAudits",
            column: "PawnTicketId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PawnTicketEditAudits");
    }
}
