using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreInteractionContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedAreaResRefsJson",
                table: "InteractionDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequiredIndustryTagsJson",
                table: "InteractionDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequiredKnowledgeTagsJson",
                table: "InteractionDefinitions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedAreaResRefsJson",
                table: "InteractionDefinitions");

            migrationBuilder.DropColumn(
                name: "RequiredIndustryTagsJson",
                table: "InteractionDefinitions");

            migrationBuilder.DropColumn(
                name: "RequiredKnowledgeTagsJson",
                table: "InteractionDefinitions");
        }
    }
}
