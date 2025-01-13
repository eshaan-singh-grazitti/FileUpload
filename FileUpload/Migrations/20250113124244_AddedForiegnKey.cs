using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class AddedForiegnKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExcelChanges_FileId",
                table: "ExcelChanges",
                column: "FileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExcelChanges_FileUploadModal_FileId",
                table: "ExcelChanges",
                column: "FileId",
                principalTable: "FileUploadModal",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExcelChanges_FileUploadModal_FileId",
                table: "ExcelChanges");

            migrationBuilder.DropIndex(
                name: "IX_ExcelChanges_FileId",
                table: "ExcelChanges");
        }
    }
}
