using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddWorldEngineFunctionality : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemBlueprints",
                columns: table => new
                {
                    res_ref = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    item_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    base_item_type = table.Column<int>(type: "integer", nullable: false),
                    base_value = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    weight_increase_constant = table.Column<int>(type: "integer", nullable: false, defaultValue: -1),
                    job_system_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "None"),
                    materials = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    appearance = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    local_variables = table.Column<string>(type: "jsonb", nullable: true),
                    source_file = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemBlueprints", x => x.res_ref);
                });

            migrationBuilder.CreateTable(
                name: "ResourceNodeDefinitions",
                columns: table => new
                {
                    tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    plc_appearance = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Undefined"),
                    uses = table.Column<int>(type: "integer", nullable: false, defaultValue: 50),
                    base_harvest_rounds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    requirement = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    outputs = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    flora_properties = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceNodeDefinitions", x => x.tag);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemBlueprints_ItemTag",
                table: "ItemBlueprints",
                column: "item_tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemBlueprints_JobSystemType",
                table: "ItemBlueprints",
                column: "job_system_type");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBlueprints_Name",
                table: "ItemBlueprints",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceNodeDefinitions_Name",
                table: "ResourceNodeDefinitions",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceNodeDefinitions_Type",
                table: "ResourceNodeDefinitions",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemBlueprints");

            migrationBuilder.DropTable(
                name: "ResourceNodeDefinitions");
        }
    }
}
