using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddBikeDocuments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bikeDocuments",
            columns: table => new
            {
                bikeId = table.Column<string>(type: "TEXT", nullable: false),
                fileId = table.Column<string>(type: "TEXT", nullable: false),
                fileType = table.Column<string>(type: "TEXT", nullable: false),
                contentType = table.Column<string>(type: "TEXT", nullable: false),
                content = table.Column<byte[]>(type: "BLOB", nullable: false),
                addedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                sourceUpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                savedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bikeDocuments", x => new { x.bikeId, x.fileId });
                table.ForeignKey(
                    name: "FK_bikeDocuments_bikes_bikeId",
                    column: x => x.bikeId,
                    principalTable: "bikes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bikeDocuments");
    }
}
