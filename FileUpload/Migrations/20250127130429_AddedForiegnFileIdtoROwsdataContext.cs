using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class AddedForiegnFileIdtoROwsdataContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FileId",
                table: "RowsData",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "RowsData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RowsData_FileId",
                table: "RowsData",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_RowsData_FileUploadModal_FileId",
                table: "RowsData",
                column: "FileId",
                principalTable: "FileUploadModal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RowsData_FileUploadModal_FileId",
                table: "RowsData");

            migrationBuilder.DropIndex(
                name: "IX_RowsData_FileId",
                table: "RowsData");

            migrationBuilder.DropColumn(
                name: "FileId",
                table: "RowsData");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "RowsData");
        }
    }
}
