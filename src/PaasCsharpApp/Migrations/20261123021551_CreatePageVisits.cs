using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaasCsharpApp.Migrations;

/// <inheritdoc />
public partial class _20261123021551_CreatePageVisits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "page_visits",
            columns: table => new
            {
                path = table.Column<string>(type: "text", nullable: false),
                total = table.Column<long>(type: "bigint", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_page_visits", x => x.path);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "page_visits");
    }
}
