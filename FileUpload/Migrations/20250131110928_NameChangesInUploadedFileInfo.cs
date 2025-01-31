using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class NameChangesInUploadedFileInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Filename",
                table: "UploadedFileInfo",
                newName: "FileName");

            migrationBuilder.RenameColumn(
                name: "OriginalFilename",
                table: "UploadedFileInfo",
                newName: "FilenameWithTimeStamp");

            migrationBuilder.RenameColumn(
                name: "Data",
                table: "UploadedFileInfo",
                newName: "FilePathOutsideProject");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "UploadedFileInfo",
                newName: "Filename");

            migrationBuilder.RenameColumn(
                name: "FilenameWithTimeStamp",
                table: "UploadedFileInfo",
                newName: "OriginalFilename");

            migrationBuilder.RenameColumn(
                name: "FilePathOutsideProject",
                table: "UploadedFileInfo",
                newName: "Data");
        }
    }
}
