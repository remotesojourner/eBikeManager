using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddBikeDetails : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "detailsUpdatedAt",
            table: "bikes",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "hasFlowPlus",
            table: "bikes",
            type: "INTEGER",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "locationJson",
            table: "bikes",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "passJson",
            table: "bikes",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "profileJson",
            table: "bikes",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "stateOfChargeJson",
            table: "bikes",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "detailsUpdatedAt",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "hasFlowPlus",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "locationJson",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "passJson",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "profileJson",
            table: "bikes");

        migrationBuilder.DropColumn(
            name: "stateOfChargeJson",
            table: "bikes");
    }
}
