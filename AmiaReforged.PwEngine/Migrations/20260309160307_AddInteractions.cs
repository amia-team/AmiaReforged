using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InteractionDefinitions",
                columns: table => new
                {
                    tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    target_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    base_rounds = table.Column<int>(type: "integer", nullable: false),
                    min_rounds = table.Column<int>(type: "integer", nullable: false),
                    proficiency_reduces_rounds = table.Column<bool>(type: "boolean", nullable: false),
                    requires_industry_membership = table.Column<bool>(type: "boolean", nullable: false),
                    responses = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractionDefinitions", x => x.tag);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InteractionDefinitions_Name",
                table: "InteractionDefinitions",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InteractionDefinitions");
        }
    }
}
