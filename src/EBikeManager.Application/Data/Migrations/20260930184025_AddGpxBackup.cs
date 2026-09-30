using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddGpxBackup : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "gpxDownloadedAt",
            table: "rides",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "gpxPath",
            table: "rides",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "gpxUnavailable",
            table: "rides",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "gpxDownloadedAt",
            table: "rides");

        migrationBuilder.DropColumn(
            name: "gpxPath",
            table: "rides");

        migrationBuilder.DropColumn(
            name: "gpxUnavailable",
            table: "rides");
    }
}
