using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecipeTemplateDefinitions",
                columns: table => new
                {
                    tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    industry_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    required_knowledge = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    required_proficiency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ingredients = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    products = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    crafting_time_seconds = table.Column<int>(type: "integer", nullable: true),
                    knowledge_points_awarded = table.Column<int>(type: "integer", nullable: false),
                    required_workstation = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    process_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeTemplateDefinitions", x => x.tag);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeTemplateDefinitions_IndustryTag",
                table: "RecipeTemplateDefinitions",
                column: "industry_tag");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeTemplateDefinitions_Name",
                table: "RecipeTemplateDefinitions",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecipeTemplateDefinitions");
        }
    }
}
