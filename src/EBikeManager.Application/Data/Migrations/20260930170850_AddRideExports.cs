using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddRideExports : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "rideExports",
            columns: table => new
            {
                rideId = table.Column<string>(type: "TEXT", nullable: false),
                integration = table.Column<string>(type: "TEXT", nullable: false),
                status = table.Column<string>(type: "TEXT", nullable: false),
                remoteId = table.Column<string>(type: "TEXT", nullable: true),
                note = table.Column<string>(type: "TEXT", nullable: true),
                problem = table.Column<string>(type: "TEXT", nullable: true),
                attempts = table.Column<int>(type: "INTEGER", nullable: false),
                lastAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                exportedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rideExports", x => new { x.rideId, x.integration });
                table.ForeignKey(
                    name: "FK_rideExports_rides_rideId",
                    column: x => x.rideId,
                    principalTable: "rides",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "rideExports");
    }
}
