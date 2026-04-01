using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "codex_quests",
                columns: table => new
                {
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quest_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    current_stage_id = table.Column<int>(type: "integer", nullable: false),
                    date_started = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    date_completed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    quest_giver = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    keywords = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    stages_json = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    source_template_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expiry_behavior = table.Column<int>(type: "integer", nullable: true),
                    completion_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("codex_quests_pkey", x => new { x.character_id, x.quest_id });
                    table.ForeignKey(
                        name: "codex_quests_character_id_fkey",
                        column: x => x.character_id,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "codex_quests_character_id_idx",
                table: "codex_quests",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "codex_quests_character_state_idx",
                table: "codex_quests",
                columns: new[] { "character_id", "state" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "codex_quests");
        }
    }
}
