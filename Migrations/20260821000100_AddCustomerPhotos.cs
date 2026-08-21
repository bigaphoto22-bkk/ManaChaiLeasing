using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260821000100_AddCustomerPhotos")]
public partial class AddCustomerPhotos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomerPhotos",
            columns: table => new
            {
                CustomerId = table.Column<int>(
                    type: "INTEGER",
                    nullable: false),
                PhotoData = table.Column<byte[]>(
                    type: "BLOB",
                    nullable: false),
                MimeType = table.Column<string>(
                    type: "TEXT",
                    maxLength: 50,
                    nullable: false),
                Width = table.Column<int>(
                    type: "INTEGER",
                    nullable: false),
                Height = table.Column<int>(
                    type: "INTEGER",
                    nullable: false),
                ByteLength = table.Column<int>(
                    type: "INTEGER",
                    nullable: false),
                Source = table.Column<string>(
                    type: "TEXT",
                    maxLength: 30,
                    nullable: false),
                UpdatedAt = table.Column<DateTime>(
                    type: "TEXT",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_CustomerPhotos",
                    x => x.CustomerId);

                table.ForeignKey(
                    name: "FK_CustomerPhotos_Customers_CustomerId",
                    column: x => x.CustomerId,
                    principalTable: "Customers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CustomerPhotos");
    }
}
