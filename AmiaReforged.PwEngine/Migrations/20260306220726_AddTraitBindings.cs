using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddTraitBindings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "GlyphDefinitions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Encounter");

            migrationBuilder.CreateTable(
                name: "TraitGlyphBindings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    trait_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    glyph_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraitGlyphBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TraitGlyphBindings_GlyphDefinitions_glyph_definition_id",
                        column: x => x.glyph_definition_id,
                        principalTable: "GlyphDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TraitGlyphBindings_glyph_definition_id",
                table: "TraitGlyphBindings",
                column: "glyph_definition_id");

            migrationBuilder.CreateIndex(
                name: "IX_TraitGlyphBindings_trait_tag_glyph_definition_id",
                table: "TraitGlyphBindings",
                columns: new[] { "trait_tag", "glyph_definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TraitGlyphBindings");

            migrationBuilder.DropColumn(
                name: "category",
                table: "GlyphDefinitions");
        }
    }
}
