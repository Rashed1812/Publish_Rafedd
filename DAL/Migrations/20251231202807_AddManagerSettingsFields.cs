using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerSettingsFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultView",
                table: "Managers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EmailOnEmployeeJoin",
                table: "Managers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailOnTaskDeadline",
                table: "Managers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailOnTaskReport",
                table: "Managers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EmailOnWeeklyReport",
                table: "Managers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowEmployeePerformance",
                table: "Managers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowMonthlyStats",
                table: "Managers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowWeeklyStats",
                table: "Managers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TimeZone",
                table: "Managers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WeekStartDay",
                table: "Managers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WorkingHoursEnd",
                table: "Managers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WorkingHoursStart",
                table: "Managers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultView",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "EmailOnEmployeeJoin",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "EmailOnTaskDeadline",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "EmailOnTaskReport",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "EmailOnWeeklyReport",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "ShowEmployeePerformance",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "ShowMonthlyStats",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "ShowWeeklyStats",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "TimeZone",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "WeekStartDay",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "WorkingHoursEnd",
                table: "Managers");

            migrationBuilder.DropColumn(
                name: "WorkingHoursStart",
                table: "Managers");
        }
    }
}
