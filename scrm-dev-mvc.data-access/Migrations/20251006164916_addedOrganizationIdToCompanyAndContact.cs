using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class addedOrganizationIdToCompanyAndContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "contact",
                type: "int",
                nullable: true
                );

            migrationBuilder.AddColumn<int>(
                name: "OrganizationId",
                table: "company",
                type: "int",
                nullable: true
                );

            migrationBuilder.CreateIndex(
                name: "IX_contact_OrganizationId",
                table: "contact",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_company_OrganizationId",
                table: "company",
                column: "OrganizationId");

            migrationBuilder.AddForeignKey(
                name: "FK_company_organization_OrganizationId",
                table: "company",
                column: "OrganizationId",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_contact_organization_OrganizationId",
                table: "contact",
                column: "OrganizationId",
                principalTable: "organization",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_company_organization_OrganizationId",
                table: "company");

            migrationBuilder.DropForeignKey(
                name: "FK_contact_organization_OrganizationId",
                table: "contact");

            migrationBuilder.DropIndex(
                name: "IX_contact_OrganizationId",
                table: "contact");

            migrationBuilder.DropIndex(
                name: "IX_company_OrganizationId",
                table: "company");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "contact");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "company");
        }
    }
}
