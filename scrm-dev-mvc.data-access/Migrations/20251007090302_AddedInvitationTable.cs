using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace scrm_dev_mvc.data_access.Migrations
{
    /// <inheritdoc />
    public partial class AddedInvitationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invitation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    invitation_code = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    organization_id = table.Column<int>(type: "int", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    sent_date = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())"),
                    expiry_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    is_accepted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invitation", x => x.id);
                    table.ForeignKey(
                        name: "FK_invitation_organization",
                        column: x => x.organization_id,
                        principalTable: "organization",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invitation_invitation_code",
                table: "invitation",
                column: "invitation_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invitation_organization_id",
                table: "invitation",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invitation");
        }
    }
}
