using Microsoft.EntityFrameworkCore.Migrations;


namespace EBikeManager.Application.Data.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "bikes",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT", nullable: false),
                name = table.Column<string>(type: "TEXT", nullable: false),
                addedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_bikes", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "config",
            columns: table => new
            {
                key = table.Column<string>(type: "TEXT", nullable: false),
                value = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_config", x => x.key);
            });

        migrationBuilder.CreateTable(
            name: "rides",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT", nullable: false),
                bikeId = table.Column<string>(type: "TEXT", nullable: false),
                title = table.Column<string>(type: "TEXT", nullable: true),
                startTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                endTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                timeZone = table.Column<string>(type: "TEXT", nullable: true),
                distanceMeters = table.Column<int>(type: "INTEGER", nullable: true),
                movingSeconds = table.Column<int>(type: "INTEGER", nullable: true),
                caloriesKcal = table.Column<double>(type: "REAL", nullable: true),
                elevationGainMeters = table.Column<int>(type: "INTEGER", nullable: true),
                averageSpeedKmh = table.Column<double>(type: "REAL", nullable: true),
                riderEnergySharePercent = table.Column<int>(type: "INTEGER", nullable: true),
                averageRiderPowerWatts = table.Column<double>(type: "REAL", nullable: true),
                summaryJson = table.Column<string>(type: "TEXT", nullable: false, defaultValue: "{}"),
                firstSeenAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                fitPath = table.Column<string>(type: "TEXT", nullable: true),
                fitSha256 = table.Column<string>(type: "TEXT", nullable: true),
                fitSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                fitDownloadedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                fitUnavailable = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                fitError = table.Column<string>(type: "TEXT", nullable: true),
                fitTimerSeconds = table.Column<double>(type: "REAL", nullable: true),
                fitDistanceMeters = table.Column<double>(type: "REAL", nullable: true),
                fitAveragePowerWatts = table.Column<int>(type: "INTEGER", nullable: true),
                fitHasGps = table.Column<bool>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rides", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "secrets",
            columns: table => new
            {
                name = table.Column<string>(type: "TEXT", nullable: false),
                value = table.Column<string>(type: "TEXT", nullable: false),
                updatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_secrets", x => x.name);
            });

        migrationBuilder.CreateIndex(
            name: "IX_rides_startTime",
            table: "rides",
            column: "startTime");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "bikes");

        migrationBuilder.DropTable(
            name: "config");

        migrationBuilder.DropTable(
            name: "rides");

        migrationBuilder.DropTable(
            name: "secrets");
    }
}
