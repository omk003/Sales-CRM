using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class addedNameFieldInDealTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "deal",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Name",
                table: "deal");
        }
    }
}
