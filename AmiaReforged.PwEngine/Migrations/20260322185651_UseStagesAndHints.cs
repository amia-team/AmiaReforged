using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class UseStagesAndHints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HintsJson",
                table: "CodexQuestDefinitions");

            migrationBuilder.RenameColumn(
                name: "ObjectivesJson",
                table: "CodexQuestDefinitions",
                newName: "StagesJson");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StagesJson",
                table: "CodexQuestDefinitions",
                newName: "ObjectivesJson");

            migrationBuilder.AddColumn<string>(
                name: "HintsJson",
                table: "CodexQuestDefinitions",
                type: "text",
                nullable: true);
        }
    }
}
