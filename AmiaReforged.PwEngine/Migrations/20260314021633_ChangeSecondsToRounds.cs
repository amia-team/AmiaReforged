using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSecondsToRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "crafting_time_seconds",
                table: "RecipeTemplateDefinitions",
                newName: "crafting_time_rounds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "crafting_time_rounds",
                table: "RecipeTemplateDefinitions",
                newName: "crafting_time_seconds");
        }
    }
}
