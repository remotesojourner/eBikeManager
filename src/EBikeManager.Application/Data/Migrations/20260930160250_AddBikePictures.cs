using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddBikePictures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bikePictures",
            columns: table => new
            {
                bikeId = table.Column<string>(type: "TEXT", nullable: false),
                sourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                contentType = table.Column<string>(type: "TEXT", nullable: false),
                content = table.Column<byte[]>(type: "BLOB", nullable: false),
                savedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bikePictures", x => x.bikeId);
                table.ForeignKey(
                    name: "FK_bikePictures_bikes_bikeId",
                    column: x => x.bikeId,
                    principalTable: "bikes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bikePictures");
    }
}
