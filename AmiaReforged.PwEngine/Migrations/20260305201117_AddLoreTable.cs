using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddLoreTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "codex_lore_definitions",
                columns: table => new
                {
                    lore_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    keywords = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("codex_lore_definitions_pkey", x => x.lore_id);
                });

            migrationBuilder.CreateTable(
                name: "codex_lore_unlocks",
                columns: table => new
                {
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lore_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_discovered = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    discovery_location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    discovery_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("codex_lore_unlocks_pkey", x => new { x.character_id, x.lore_id });
                    table.ForeignKey(
                        name: "codex_lore_unlocks_character_id_fkey",
                        column: x => x.character_id,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "codex_lore_unlocks_lore_id_fkey",
                        column: x => x.lore_id,
                        principalTable: "codex_lore_definitions",
                        principalColumn: "lore_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "codex_lore_definitions_category_idx",
                table: "codex_lore_definitions",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "codex_lore_definitions_tier_idx",
                table: "codex_lore_definitions",
                column: "tier");

            migrationBuilder.CreateIndex(
                name: "codex_lore_unlocks_character_id_idx",
                table: "codex_lore_unlocks",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "IX_codex_lore_unlocks_lore_id",
                table: "codex_lore_unlocks",
                column: "lore_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "codex_lore_unlocks");

            migrationBuilder.DropTable(
                name: "codex_lore_definitions");
        }
    }
}
