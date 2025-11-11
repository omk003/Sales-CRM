using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class addedSenderDetailsInInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SenderId",
                table: "invitation",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SenderId",
                table: "invitation");
        }
    }
}
