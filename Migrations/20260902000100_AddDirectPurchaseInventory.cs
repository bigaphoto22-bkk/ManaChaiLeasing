using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260902000100_AddDirectPurchaseInventory")]
public partial class AddDirectPurchaseInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DirectPurchases",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DocumentNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                PurchaseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                PurchasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                Status = table.Column<string>(type: "TEXT", nullable: false),
                SellerCustomerId = table.Column<int>(type: "INTEGER", nullable: false),
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
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Note = table.Column<string>(type: "TEXT", maxLength: 1500, nullable: true),
                CancellationReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DirectPurchases", x => x.Id);
                table.ForeignKey(
                    name: "FK_DirectPurchases_Customers_SellerCustomerId",
                    column: x => x.SellerCustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "DirectPurchaseEditAudits",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DirectPurchaseId = table.Column<int>(type: "INTEGER", nullable: false),
                EditedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                EditorUser = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                EditorMachine = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                ChangeSummary = table.Column<string>(type: "TEXT", maxLength: 12000, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DirectPurchaseEditAudits", x => x.Id);
                table.ForeignKey(
                    name: "FK_DirectPurchaseEditAudits_DirectPurchases_DirectPurchaseId",
                    column: x => x.DirectPurchaseId,
                    principalTable: "DirectPurchases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DirectPurchaseTransactions",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                DirectPurchaseId = table.Column<int>(type: "INTEGER", nullable: false),
                TransactionType = table.Column<string>(type: "TEXT", nullable: false),
                CashFlowType = table.Column<string>(type: "TEXT", nullable: false),
                TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                PaymentMethod = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                IsVoided = table.Column<bool>(type: "INTEGER", nullable: false),
                VoidReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DirectPurchaseTransactions", x => x.Id);
                table.ForeignKey(
                    name: "FK_DirectPurchaseTransactions_DirectPurchases_DirectPurchaseId",
                    column: x => x.DirectPurchaseId,
                    principalTable: "DirectPurchases",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_DirectPurchaseEditAudits_DirectPurchaseId", table: "DirectPurchaseEditAudits", column: "DirectPurchaseId");
        migrationBuilder.CreateIndex(name: "IX_DirectPurchases_DocumentNumber", table: "DirectPurchases", column: "DocumentNumber", unique: true);
        migrationBuilder.CreateIndex(name: "IX_DirectPurchases_SellerCustomerId", table: "DirectPurchases", column: "SellerCustomerId");
        migrationBuilder.CreateIndex(name: "IX_DirectPurchaseTransactions_DirectPurchaseId", table: "DirectPurchaseTransactions", column: "DirectPurchaseId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DirectPurchaseEditAudits");
        migrationBuilder.DropTable(name: "DirectPurchaseTransactions");
        migrationBuilder.DropTable(name: "DirectPurchases");
    }
}
