using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddGlyphSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlyphDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    graph_json = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlyphDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpawnProfileGlyphBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    glyph_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnProfileGlyphBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnProfileGlyphBindings_GlyphDefinitions_glyph_definition~",
                        column: x => x.glyph_definition_id,
                        principalTable: "GlyphDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpawnProfileGlyphBindings_SpawnProfiles_spawn_profile_id",
                        column: x => x.spawn_profile_id,
                        principalTable: "SpawnProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpawnProfileGlyphBindings_glyph_definition_id",
                table: "SpawnProfileGlyphBindings",
                column: "glyph_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnProfileGlyphBindings_Profile_Definition",
                table: "SpawnProfileGlyphBindings",
                columns: new[] { "spawn_profile_id", "glyph_definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpawnProfileGlyphBindings");

            migrationBuilder.DropTable(
                name: "GlyphDefinitions");
        }
    }
}
