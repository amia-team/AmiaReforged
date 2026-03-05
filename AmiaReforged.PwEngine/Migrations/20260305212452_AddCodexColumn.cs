using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddCodexColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_always_available",
                table: "codex_lore_definitions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "codex_lore_definitions_always_available_idx",
                table: "codex_lore_definitions",
                column: "is_always_available");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "codex_lore_definitions_always_available_idx",
                table: "codex_lore_definitions");

            migrationBuilder.DropColumn(
                name: "is_always_available",
                table: "codex_lore_definitions");
        }
    }
}
