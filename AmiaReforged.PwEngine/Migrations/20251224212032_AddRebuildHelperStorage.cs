using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddRebuildHelperStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character_rebuilds",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_cd_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    character_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    completed_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("character_rebuilds_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_character_rebuilds_PersistedCharacter_character_id",
                        column: x => x.character_id,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_character_rebuilds_player_personas_player_cd_key",
                        column: x => x.player_cd_key,
                        principalTable: "player_personas",
                        principalColumn: "cd_key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rebuild_item_records",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    character_rebuild_id = table.Column<int>(type: "integer", nullable: false),
                    item_data = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("rebuild_item_records_pkey", x => x.id);
                    table.ForeignKey(
                        name: "FK_rebuild_item_records_character_rebuilds_character_rebuild_id",
                        column: x => x.character_rebuild_id,
                        principalTable: "character_rebuilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "character_rebuilds_character_id_idx",
                table: "character_rebuilds",
                column: "character_id");

            migrationBuilder.CreateIndex(
                name: "character_rebuilds_player_cd_key_idx",
                table: "character_rebuilds",
                column: "player_cd_key");

            migrationBuilder.CreateIndex(
                name: "rebuild_item_records_character_rebuild_id_idx",
                table: "rebuild_item_records",
                column: "character_rebuild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rebuild_item_records");

            migrationBuilder.DropTable(
                name: "character_rebuilds");
        }
    }
}
