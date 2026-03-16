using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "knowledge_points_awarded",
                table: "RecipeTemplateDefinitions",
                newName: "progression_points_awarded");

            migrationBuilder.CreateTable(
                name: "KnowledgeCapProfiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    soft_cap = table.Column<int>(type: "integer", nullable: false, defaultValue: 100),
                    hard_cap = table.Column<int>(type: "integer", nullable: false, defaultValue: 150)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeCapProfiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeProgressions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    economy_earned_knowledge_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    level_up_knowledge_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    accumulated_progression_points = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cap_profile_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeProgressions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeCapProfiles_tag",
                table: "KnowledgeCapProfiles",
                column: "tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeProgressions_character_id",
                table: "KnowledgeProgressions",
                column: "character_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeCapProfiles");

            migrationBuilder.DropTable(
                name: "KnowledgeProgressions");

            migrationBuilder.RenameColumn(
                name: "progression_points_awarded",
                table: "RecipeTemplateDefinitions",
                newName: "knowledge_points_awarded");
        }
    }
}
