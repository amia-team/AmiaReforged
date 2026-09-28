using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace AmiaReforged.PwEngine.Migrations;

public partial class GlyphSourcePrograms : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "graph_json", table: "GlyphDefinitions");
        migrationBuilder.AddColumn<string>(name: "source_text", table: "GlyphDefinitions", type: "text", nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<int>(name: "language_version", table: "GlyphDefinitions", type: "integer", nullable: false, defaultValue: 1);
        migrationBuilder.AddColumn<string>(name: "published_versions_json", table: "GlyphDefinitions", type: "text", nullable: false, defaultValue: "[]");
        // Prototype definitions have no source deployment. There is intentionally no graph migration frontend.
        migrationBuilder.Sql("UPDATE \"GlyphDefinitions\" SET is_active = FALSE;");
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "source_text", table: "GlyphDefinitions");
        migrationBuilder.DropColumn(name: "language_version", table: "GlyphDefinitions");
        migrationBuilder.DropColumn(name: "published_versions_json", table: "GlyphDefinitions");
        migrationBuilder.AddColumn<string>(name: "graph_json", table: "GlyphDefinitions", type: "text", nullable: false, defaultValue: "{}");
        migrationBuilder.Sql("UPDATE \"GlyphDefinitions\" SET is_active = FALSE;");
    }
}
