using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class excelCganesDeletedContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isDeleted",
                table: "ExcelChanges",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isDeleted",
                table: "ExcelChanges");
        }
    }
}
