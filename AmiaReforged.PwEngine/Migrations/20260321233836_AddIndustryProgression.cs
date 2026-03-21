using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustryProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProficiencyXpAwarded",
                table: "RecipeTemplateDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProficiencyXp",
                table: "IndustryMemberships",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ProficiencyXpLevel",
                table: "IndustryMemberships",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProficiencyXpAwarded",
                table: "RecipeTemplateDefinitions");

            migrationBuilder.DropColumn(
                name: "ProficiencyXp",
                table: "IndustryMemberships");

            migrationBuilder.DropColumn(
                name: "ProficiencyXpLevel",
                table: "IndustryMemberships");
        }
    }
}
