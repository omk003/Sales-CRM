using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class ChangedTaskTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "task",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContactId",
                table: "task",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DealId",
                table: "task",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_task_CompanyId",
                table: "task",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_task_ContactId",
                table: "task",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_task_DealId",
                table: "task",
                column: "DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_task_company_CompanyId",
                table: "task",
                column: "CompanyId",
                principalTable: "company",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_contact_ContactId",
                table: "task",
                column: "ContactId",
                principalTable: "contact",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_task_deal_DealId",
                table: "task",
                column: "DealId",
                principalTable: "deal",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_task_company_CompanyId",
                table: "task");

            migrationBuilder.DropForeignKey(
                name: "FK_task_contact_ContactId",
                table: "task");

            migrationBuilder.DropForeignKey(
                name: "FK_task_deal_DealId",
                table: "task");

            migrationBuilder.DropIndex(
                name: "IX_task_CompanyId",
                table: "task");

            migrationBuilder.DropIndex(
                name: "IX_task_ContactId",
                table: "task");

            migrationBuilder.DropIndex(
                name: "IX_task_DealId",
                table: "task");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "task");

            migrationBuilder.DropColumn(
                name: "ContactId",
                table: "task");

            migrationBuilder.DropColumn(
                name: "DealId",
                table: "task");
        }
    }
}
