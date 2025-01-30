using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class TottalyChangeTheRowsDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Column1",
                table: "RowsData");

            migrationBuilder.DropColumn(
                name: "Column2",
                table: "RowsData");

            migrationBuilder.RenameColumn(
                name: "Column6",
                table: "RowsData",
                newName: "Segment");

            migrationBuilder.RenameColumn(
                name: "Column5",
                table: "RowsData",
                newName: "Product");

            migrationBuilder.RenameColumn(
                name: "Column4",
                table: "RowsData",
                newName: "DiscountBand");

            migrationBuilder.RenameColumn(
                name: "Column3",
                table: "RowsData",
                newName: "Country");

            migrationBuilder.AlterColumn<int>(
                name: "RowId",
                table: "RowsData",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "ManufacturingPrice",
                table: "RowsData",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UnitsSold",
                table: "RowsData",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManufacturingPrice",
                table: "RowsData");

            migrationBuilder.DropColumn(
                name: "UnitsSold",
                table: "RowsData");

            migrationBuilder.RenameColumn(
                name: "Segment",
                table: "RowsData",
                newName: "Column6");

            migrationBuilder.RenameColumn(
                name: "Product",
                table: "RowsData",
                newName: "Column5");

            migrationBuilder.RenameColumn(
                name: "DiscountBand",
                table: "RowsData",
                newName: "Column4");

            migrationBuilder.RenameColumn(
                name: "Country",
                table: "RowsData",
                newName: "Column3");

            migrationBuilder.AlterColumn<string>(
                name: "RowId",
                table: "RowsData",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Column1",
                table: "RowsData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Column2",
                table: "RowsData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
