using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddTraitDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "trait_definitions",
                columns: table => new
                {
                    tag = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    point_cost = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    death_behavior = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    requires_unlock = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    dm_only = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    effects = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    allowed_races = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    allowed_classes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    forbidden_races = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    forbidden_classes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    conflicting_traits = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    prerequisite_traits = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("trait_definitions_pkey", x => x.tag);
                });

            migrationBuilder.CreateIndex(
                name: "trait_definitions_category_idx",
                table: "trait_definitions",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "trait_definitions_death_behavior_idx",
                table: "trait_definitions",
                column: "death_behavior");

            migrationBuilder.CreateIndex(
                name: "trait_definitions_dm_only_idx",
                table: "trait_definitions",
                column: "dm_only");

            migrationBuilder.CreateIndex(
                name: "trait_definitions_requires_unlock_idx",
                table: "trait_definitions",
                column: "requires_unlock");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trait_definitions");
        }
    }
}
