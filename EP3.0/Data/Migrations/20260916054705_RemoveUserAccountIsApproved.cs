using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EarnPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserAccountIsApproved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "UserAccounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "UserAccounts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
