using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogTrees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dialogue_trees",
                columns: table => new
                {
                    dialogue_tree_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    root_node_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true),
                    speaker_tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    nodes_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("dialogue_trees_pkey", x => x.dialogue_tree_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_dialogue_trees_speaker_tag",
                table: "dialogue_trees",
                column: "speaker_tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dialogue_trees");
        }
    }
}
