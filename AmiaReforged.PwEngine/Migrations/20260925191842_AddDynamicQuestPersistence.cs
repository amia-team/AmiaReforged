using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicQuestPersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dynamic_quest_templates",
                columns: table => new
                {
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dynamic_quest_templates", x => x.template_id);
                });

            migrationBuilder.CreateTable(
                name: "dynamic_quest_completions",
                columns: table => new
                {
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completion_count = table.Column<int>(type: "integer", nullable: false),
                    last_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dynamic_quest_completions", x => new { x.character_id, x.template_id });
                    table.ForeignKey(
                        name: "FK_dynamic_quest_completions_dynamic_quest_templates_template_~",
                        column: x => x.template_id,
                        principalTable: "dynamic_quest_templates",
                        principalColumn: "template_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "dynamic_quest_postings",
                columns: table => new
                {
                    posting_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    posted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dynamic_quest_postings", x => x.posting_id);
                    table.ForeignKey(
                        name: "FK_dynamic_quest_postings_dynamic_quest_templates_source_templ~",
                        column: x => x.source_template_id,
                        principalTable: "dynamic_quest_templates",
                        principalColumn: "template_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_dynamic_quest_completions_template_id",
                table: "dynamic_quest_completions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_dynamic_quest_postings_expires_at",
                table: "dynamic_quest_postings",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "IX_dynamic_quest_postings_source_template_id",
                table: "dynamic_quest_postings",
                column: "source_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_dynamic_quest_templates_is_active_source",
                table: "dynamic_quest_templates",
                columns: new[] { "is_active", "source" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dynamic_quest_completions");

            migrationBuilder.DropTable(
                name: "dynamic_quest_postings");

            migrationBuilder.DropTable(
                name: "dynamic_quest_templates");
        }
    }
}
