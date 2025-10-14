using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class changedUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "user");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCheckedTime",
                table: "user",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastProcessedUid",
                table: "user",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedTime",
                table: "user");

            migrationBuilder.DropColumn(
                name: "LastProcessedUid",
                table: "user");

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "user",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
