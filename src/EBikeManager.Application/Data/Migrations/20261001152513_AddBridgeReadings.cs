using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddBridgeReadings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "bridgeBatteryAt",
            table: "bikes",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "bridgeBatteryPercent",
            table: "bikes",
            type: "REAL",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "bridgeOdometerAt",
            table: "bikes",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<double>(
            name: "bridgeOdometerKm",
            table: "bikes",
            type: "REAL",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bridgeBatteryAt",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "bridgeBatteryPercent",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "bridgeOdometerAt",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "bridgeOdometerKm",
            table: "bikes");
    }
}
