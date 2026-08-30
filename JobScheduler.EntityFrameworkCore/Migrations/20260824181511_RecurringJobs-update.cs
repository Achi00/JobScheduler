using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JobScheduler.EntityFrameworkCore.Migrations
{
    /// <inheritdoc />
    public partial class RecurringJobsupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                table: "RecurringJob",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "JobType",
                table: "RecurringJob",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CronExpression",
                table: "RecurringJob",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "RecurringJob",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_RecurringJob_IsEnabled_NextRunAt",
                table: "RecurringJob",
                columns: new[] { "IsEnabled", "NextRunAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecurringJob_IsEnabled_NextRunAt",
                table: "RecurringJob");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "RecurringJob");

            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                table: "RecurringJob",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "JobType",
                table: "RecurringJob",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "CronExpression",
                table: "RecurringJob",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
