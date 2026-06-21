using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResourceMindAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TimesheetSubmissionIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimesheetSubmissionIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WeekStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FirstReminderSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SecondReminderSentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FrozenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RestoredByManagerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimesheetSubmissionIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimesheetSubmissionIssues_Users_EmployeeUserId",
                        column: x => x.EmployeeUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetSubmissionIssues_Users_ManagerUserId",
                        column: x => x.ManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimesheetSubmissionIssues_Users_RestoredByManagerUserId",
                        column: x => x.RestoredByManagerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetSubmissionIssues_EmployeeUserId_WeekStartDate",
                table: "TimesheetSubmissionIssues",
                columns: new[] { "EmployeeUserId", "WeekStartDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetSubmissionIssues_ManagerUserId",
                table: "TimesheetSubmissionIssues",
                column: "ManagerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TimesheetSubmissionIssues_RestoredByManagerUserId",
                table: "TimesheetSubmissionIssues",
                column: "RestoredByManagerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimesheetSubmissionIssues");
        }
    }
}
