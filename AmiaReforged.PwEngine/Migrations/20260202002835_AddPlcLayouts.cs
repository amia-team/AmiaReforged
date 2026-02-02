using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddPlcLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plc_layout_configurations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plc_layout_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plc_layout_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    layout_configuration_id = table.Column<long>(type: "bigint", nullable: false),
                    plc_resref = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    plc_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    x = table.Column<float>(type: "real", nullable: false),
                    y = table.Column<float>(type: "real", nullable: false),
                    z = table.Column<float>(type: "real", nullable: false),
                    orientation = table.Column<float>(type: "real", nullable: false),
                    scale = table.Column<float>(type: "real", nullable: false),
                    appearance = table.Column<int>(type: "integer", nullable: false),
                    health_override = table.Column<int>(type: "integer", nullable: false),
                    is_plot = table.Column<bool>(type: "boolean", nullable: false),
                    is_static = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plc_layout_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_plc_layout_items_plc_layout_configurations_layout_configura~",
                        column: x => x.layout_configuration_id,
                        principalTable: "plc_layout_configurations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "plc_layout_configurations_character_idx",
                table: "plc_layout_configurations",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "plc_layout_configurations_property_character_idx",
                table: "plc_layout_configurations",
                columns: new[] { "property_id", "character_id" });

            migrationBuilder.CreateIndex(
                name: "plc_layout_items_layout_id_idx",
                table: "plc_layout_items",
                column: "layout_configuration_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plc_layout_items");

            migrationBuilder.DropTable(
                name: "plc_layout_configurations");
        }
    }
}
