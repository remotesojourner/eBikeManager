using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddRideBikeName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "bikeName",
            table: "rides",
            type: "TEXT",
            nullable: true);

        migrationBuilder.Sql("UPDATE rides SET bikeName = (SELECT bikes.name FROM bikes WHERE bikes.id = rides.bikeId)");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "bikeName",
            table: "rides");
    }
}
