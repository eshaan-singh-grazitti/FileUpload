using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class AddedRowsDbCorrectContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Culumn6",
                table: "RowsData",
                newName: "Column6");

            migrationBuilder.RenameColumn(
                name: "Culumn5",
                table: "RowsData",
                newName: "Column5");

            migrationBuilder.RenameColumn(
                name: "Culumn4",
                table: "RowsData",
                newName: "Column4");

            migrationBuilder.RenameColumn(
                name: "Culumn3",
                table: "RowsData",
                newName: "Column3");

            migrationBuilder.RenameColumn(
                name: "Culumn2",
                table: "RowsData",
                newName: "Column2");

            migrationBuilder.RenameColumn(
                name: "Culumn1",
                table: "RowsData",
                newName: "Column1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Column6",
                table: "RowsData",
                newName: "Culumn6");

            migrationBuilder.RenameColumn(
                name: "Column5",
                table: "RowsData",
                newName: "Culumn5");

            migrationBuilder.RenameColumn(
                name: "Column4",
                table: "RowsData",
                newName: "Culumn4");

            migrationBuilder.RenameColumn(
                name: "Column3",
                table: "RowsData",
                newName: "Culumn3");

            migrationBuilder.RenameColumn(
                name: "Column2",
                table: "RowsData",
                newName: "Culumn2");

            migrationBuilder.RenameColumn(
                name: "Column1",
                table: "RowsData",
                newName: "Culumn1");
        }
    }
}
