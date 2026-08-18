using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CitizenId = table.Column<string>(type: "TEXT", maxLength: 13, nullable: true),
                Age = table.Column<int>(type: "INTEGER", nullable: true),
                Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                Address = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Customers", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "SmartLookupValues",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                FieldType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Value = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                NormalizedValue = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                UsageCount = table.Column<int>(type: "INTEGER", nullable: false),
                LastUsedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SmartLookupValues", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "PawnTickets",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                TicketNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                PawnDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                PrincipalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                AssetCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                ProductType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Brand = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Model = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                CapacityOrSize = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                Color = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                ImeiOrSerial = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                Accessories = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                Condition = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                Specification = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                OtherDetails = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                ProductSummary = table.Column<string>(type: "TEXT", maxLength: 2500, nullable: false),
                Note = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PawnTickets", x => x.Id);
                table.ForeignKey(
                    name: "FK_PawnTickets_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "PawnTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                PawnTicketId = table.Column<int>(type: "INTEGER", nullable: false),
                TransactionType = table.Column<string>(type: "TEXT", nullable: false),
                CashFlowType = table.Column<string>(type: "TEXT", nullable: false),
                TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                InterestSequence = table.Column<int>(type: "INTEGER", nullable: true),
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                IsVoided = table.Column<bool>(type: "INTEGER", nullable: false),
                VoidReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PawnTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_PawnTransactions_PawnTickets_PawnTicketId",
                    column: x => x.PawnTicketId,
                    principalTable: "PawnTickets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Customers_CitizenId",
            table: "Customers",
            column: "CitizenId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PawnTickets_CustomerId",
            table: "PawnTickets",
            column: "CustomerId");

        migrationBuilder.CreateIndex(
            name: "IX_PawnTickets_TicketNumber",
            table: "PawnTickets",
            column: "TicketNumber",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PawnTransactions_PawnTicketId",
            table: "PawnTransactions",
            column: "PawnTicketId");

        migrationBuilder.CreateIndex(
            name: "IX_SmartLookupValues_FieldType_Category_NormalizedValue",
            table: "SmartLookupValues",
            columns: new[] { "FieldType", "Category", "NormalizedValue" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PawnTransactions");
        migrationBuilder.DropTable(name: "SmartLookupValues");
        migrationBuilder.DropTable(name: "PawnTickets");
        migrationBuilder.DropTable(name: "Customers");
    }
}
