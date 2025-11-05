using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class PersistPersonas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "owner_player_persona_id",
                table: "player_stalls",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "player_personas",
                columns: table => new
                {
                    cd_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    persona_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    last_seen_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("player_personas_pkey", x => x.cd_key);
                });

            migrationBuilder.CreateIndex(
                name: "player_stalls_owner_player_area_idx",
                table: "player_stalls",
                columns: new[] { "owner_player_persona_id", "area_resref" },
                unique: true,
                filter: "owner_player_persona_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "player_personas_persona_id_idx",
                table: "player_personas",
                column: "persona_id",
                unique: true,
                filter: "persona_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_personas");

            migrationBuilder.DropIndex(
                name: "player_stalls_owner_player_area_idx",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "owner_player_persona_id",
                table: "player_stalls");
        }
    }
}
