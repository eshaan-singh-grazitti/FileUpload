using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class AddedROwIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RowId",
                table: "RowsData",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowId",
                table: "RowsData");
        }
    }
}
