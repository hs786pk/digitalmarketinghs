using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EarnPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddReferralTrackingToUserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SuccessfulReferralCount",
                table: "UserAccounts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuccessfulReferralCount",
                table: "UserAccounts");
        }
    }
}
