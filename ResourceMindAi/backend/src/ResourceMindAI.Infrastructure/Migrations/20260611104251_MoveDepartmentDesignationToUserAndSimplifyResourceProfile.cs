using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ResourceMindAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveDepartmentDesignationToUserAndSimplifyResourceProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allocations_ResourceProfiles_ResourceProfileId",
                table: "Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_ResourceProfiles_ResourceProfileId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "ResourceProfiles");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "ResourceProfiles");

            migrationBuilder.RenameColumn(
                name: "ResourceProfileId",
                table: "Timesheets",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_ResourceProfileId_ProjectId_WeekStartDate",
                table: "Timesheets",
                newName: "IX_Timesheets_UserId_ProjectId_WeekStartDate");

            migrationBuilder.RenameColumn(
                name: "ResourceProfileId",
                table: "Allocations",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Allocations_ResourceProfileId",
                table: "Allocations",
                newName: "IX_Allocations_UserId");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "Users",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Allocations_Users_UserId",
                table: "Allocations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_Users_UserId",
                table: "Timesheets",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allocations_Users_UserId",
                table: "Allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Timesheets_Users_UserId",
                table: "Timesheets");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Designation",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Timesheets",
                newName: "ResourceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Timesheets_UserId_ProjectId_WeekStartDate",
                table: "Timesheets",
                newName: "IX_Timesheets_ResourceProfileId_ProjectId_WeekStartDate");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Allocations",
                newName: "ResourceProfileId");

            migrationBuilder.RenameIndex(
                name: "IX_Allocations_UserId",
                table: "Allocations",
                newName: "IX_Allocations_ResourceProfileId");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "ResourceProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Designation",
                table: "ResourceProfiles",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_Allocations_ResourceProfiles_ResourceProfileId",
                table: "Allocations",
                column: "ResourceProfileId",
                principalTable: "ResourceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Timesheets_ResourceProfiles_ResourceProfileId",
                table: "Timesheets",
                column: "ResourceProfileId",
                principalTable: "ResourceProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
