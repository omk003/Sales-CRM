using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class addedJobTitleInContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "contact",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "contact");
        }
    }
}
