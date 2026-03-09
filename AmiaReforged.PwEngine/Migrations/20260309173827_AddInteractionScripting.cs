using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddInteractionScripting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InteractionGlyphBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    interaction_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    area_resref = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    glyph_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InteractionGlyphBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InteractionGlyphBindings_GlyphDefinitions_glyph_definition_~",
                        column: x => x.glyph_definition_id,
                        principalTable: "GlyphDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InteractionGlyphBindings_glyph_definition_id",
                table: "InteractionGlyphBindings",
                column: "glyph_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_InteractionGlyphBindings_interaction_tag",
                table: "InteractionGlyphBindings",
                column: "interaction_tag");

            migrationBuilder.CreateIndex(
                name: "IX_InteractionGlyphBindings_interaction_tag_glyph_definition_i~",
                table: "InteractionGlyphBindings",
                columns: new[] { "interaction_tag", "glyph_definition_id", "area_resref" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InteractionGlyphBindings");
        }
    }
}
