using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EarnPlatform.Migrations;

public partial class HardenTaskAndPaymentState : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "StartedAt",
            table: "WorkTaskCompletions",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "GETUTCDATE()");

        migrationBuilder.AlterColumn<DateTime>(
            name: "CompletedAt",
            table: "WorkTaskCompletions",
            type: "datetime2",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "datetime2");

        migrationBuilder.CreateIndex(
            name: "IX_PaymentApprovals_TransactionId_UniqueActive",
            table: "PaymentApprovals",
            column: "TransactionId",
            unique: true,
            filter: "[Status] <> 'Rejected'");

        migrationBuilder.CreateIndex(
            name: "IX_WorkTaskCompletions_UserId_Status_StartedAt",
            table: "WorkTaskCompletions",
            columns: new[] { "UserId", "Status", "StartedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_PaymentApprovals_TransactionId_UniqueActive", table: "PaymentApprovals");
        migrationBuilder.DropIndex(name: "IX_WorkTaskCompletions_UserId_Status_StartedAt", table: "WorkTaskCompletions");
        migrationBuilder.AlterColumn<DateTime>(name: "CompletedAt", table: "WorkTaskCompletions", type: "datetime2", nullable: false, oldClrType: typeof(DateTime), oldType: "datetime2", oldNullable: true);
        migrationBuilder.DropColumn(name: "StartedAt", table: "WorkTaskCompletions");
    }
}
