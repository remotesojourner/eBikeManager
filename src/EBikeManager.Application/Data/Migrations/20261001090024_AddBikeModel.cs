using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddBikeModel : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bikeModel",
            table: "rides",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "model",
            table: "bikes",
            type: "TEXT",
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql("UPDATE bikes SET model = name");
        migrationBuilder.Sql("UPDATE rides SET bikeModel = bikeName");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bikeModel",
            table: "rides");

        migrationBuilder.DropColumn(
            name: "model",
            table: "bikes");
    }
}
