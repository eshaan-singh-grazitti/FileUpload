using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FileUpload.Migrations
{
    /// <inheritdoc />
    public partial class AddedRowsDbContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RowsData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Culumn1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Culumn2 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Culumn3 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Culumn4 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Culumn5 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Culumn6 = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RowsData", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RowsData");
        }
    }
}
