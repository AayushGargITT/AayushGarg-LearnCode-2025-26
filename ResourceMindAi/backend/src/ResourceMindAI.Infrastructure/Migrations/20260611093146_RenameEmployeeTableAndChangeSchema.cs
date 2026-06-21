using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResourceMindAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameEmployeeTableAndChangeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allocations_Employees_EmployeeId",
                table: "Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_Employees_EmployeeId",
                table: "Skills");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_Employees_EmployeeId",
                table: "Timesheets");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "Timesheets",
                newName: "ResourceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_EmployeeId_ProjectId_WeekStartDate",
                table: "Timesheets",
                newName: "IX_Timesheets_ResourceProfileId_ProjectId_WeekStartDate");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "Skills",
                newName: "ResourceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_EmployeeId_SkillName",
                table: "Skills",
                newName: "IX_Skills_ResourceProfileId_SkillName");

            migrationBuilder.RenameColumn(
                name: "EmployeeId",
                table: "Allocations",
                newName: "ResourceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Allocations_EmployeeId",
                table: "Allocations",
                newName: "IX_Allocations_ResourceProfileId");

            migrationBuilder.CreateTable(
                name: "ResourceProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceProfiles_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResourceProfiles_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceProfiles_ManagerId",
                table: "ResourceProfiles",
                column: "ManagerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Allocations_ResourceProfiles_ResourceProfileId",
                table: "Allocations",
                column: "ResourceProfileId",
                principalTable: "ResourceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_ResourceProfiles_ResourceProfileId",
                table: "Skills",
                column: "ResourceProfileId",
                principalTable: "ResourceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_ResourceProfiles_ResourceProfileId",
                table: "Timesheets",
                column: "ResourceProfileId",
                principalTable: "ResourceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allocations_ResourceProfiles_ResourceProfileId",
                table: "Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Skills_ResourceProfiles_ResourceProfileId",
                table: "Skills");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_ResourceProfiles_ResourceProfileId",
                table: "Timesheets");

            migrationBuilder.DropTable(
                name: "ResourceProfiles");

            migrationBuilder.RenameColumn(
                name: "ResourceProfileId",
                table: "Timesheets",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_ResourceProfileId_ProjectId_WeekStartDate",
                table: "Timesheets",
                newName: "IX_Timesheets_EmployeeId_ProjectId_WeekStartDate");

            migrationBuilder.RenameColumn(
                name: "ResourceProfileId",
                table: "Skills",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Skills_ResourceProfileId_SkillName",
                table: "Skills",
                newName: "IX_Skills_EmployeeId_SkillName");

            migrationBuilder.RenameColumn(
                name: "ResourceProfileId",
                table: "Allocations",
                newName: "EmployeeId");

            migrationBuilder.RenameIndex(
                name: "IX_Allocations_ResourceProfileId",
                table: "Allocations",
                newName: "IX_Allocations_EmployeeId");

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Designation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Users_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ManagerId",
                table: "Employees",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_UserId",
                table: "Employees",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Allocations_Employees_EmployeeId",
                table: "Allocations",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Skills_Employees_EmployeeId",
                table: "Skills",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_Employees_EmployeeId",
                table: "Timesheets",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
