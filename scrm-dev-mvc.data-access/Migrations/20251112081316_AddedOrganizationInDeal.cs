using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class AddedOrganizationInDeal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_deal_OrganizationId",
                table: "deal",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_deal_organization_OrganizationId",
                table: "deal",
                column: "OrganizationId",
                principalTable: "organization",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_deal_organization_OrganizationId",
                table: "deal");

            migrationBuilder.DropIndex(
                name: "IX_deal_OrganizationId",
                table: "deal");
        }
    }
}
