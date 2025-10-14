using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class AddedUniqueConstraintToCompanyAndContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropUniqueConstraint(
                name: "UQ__contact__AB6E616478699CE9",
                table: "contact");

            migrationBuilder.CreateIndex(
                name: "IX_contact_email_OrganizationId",
                table: "contact",
                columns: new[] { "email", "OrganizationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_company_domain_OrganizationId",
                table: "company",
                columns: new[] { "domain", "OrganizationId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_contact_email_OrganizationId",
                table: "contact");

            migrationBuilder.DropIndex(
                name: "IX_company_domain_OrganizationId",
                table: "company");

            migrationBuilder.CreateIndex(
                name: "UQ__contact__AB6E616478699CE9",
                table: "contact",
                column: "email",
                unique: true);
        }
    }
}
