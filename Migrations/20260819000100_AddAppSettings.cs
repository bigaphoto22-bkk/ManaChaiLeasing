using System;
using ManaChaiLeasing.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManaChaiLeasing.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819000100_AddAppSettings")]
public partial class AddAppSettings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AppSettings",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                StoreName = table.Column<string>(
                    type: "TEXT",
                    maxLength: 200,
                    nullable: false),
                InterestRatePercent = table.Column<decimal>(
                    type: "TEXT",
                    precision: 8,
                    scale: 2,
                    nullable: false),
                InterestPeriodDays = table.Column<int>(
                    type: "INTEGER",
                    nullable: false),
                UpdatedAt = table.Column<DateTime>(
                    type: "TEXT",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AppSettings", x => x.Id);
            });

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AppSettings");
    }
}
