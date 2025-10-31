using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureOrgMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organization_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    joined_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    departed_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    roles = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_members", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_character_id",
                table: "organization_members",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_character_id_organization_id",
                table: "organization_members",
                columns: new[] { "character_id", "organization_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_organization_id",
                table: "organization_members",
                column: "organization_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "organization_members");
        }
    }
}
