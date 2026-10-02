using Microsoft.EntityFrameworkCore.Migrations;

namespace EBikeManager.Application.Data.Migrations;

public partial class AddNotificationChannels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "notificationChannels",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT", nullable: false),
                type = table.Column<string>(type: "TEXT", nullable: false),
                displayName = table.Column<string>(type: "TEXT", nullable: false),
                data = table.Column<string>(type: "TEXT", nullable: false),
                lastActivity = table.Column<DateTime>(type: "TEXT", nullable: true),
                activityFailed = table.Column<bool>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_notificationChannels", x => x.id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "notificationChannels");
    }
}
