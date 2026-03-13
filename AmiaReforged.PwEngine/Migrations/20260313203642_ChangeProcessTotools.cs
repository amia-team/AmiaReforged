using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProcessTotools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "process_id",
                table: "RecipeTemplateDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "required_tools",
                table: "RecipeTemplateDefinitions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "required_tools",
                table: "RecipeTemplateDefinitions");

            migrationBuilder.AddColumn<string>(
                name: "process_id",
                table: "RecipeTemplateDefinitions",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }
    }
}
