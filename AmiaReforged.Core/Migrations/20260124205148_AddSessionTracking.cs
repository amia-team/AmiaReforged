using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dm_playtime_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cd_key = table.Column<string>(type: "character varying", nullable: false),
                    week_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    minutes_played = table.Column<int>(type: "integer", nullable: false),
                    minutes_toward_next_dc = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("dm_playtime_records_pkey", x => x.id);
                    table.ForeignKey(
                        name: "dm_playtime_records_cd_key_fkey",
                        column: x => x.cd_key,
                        principalTable: "Dms",
                        principalColumn: "CdKey",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_playtime_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cd_key = table.Column<string>(type: "character varying", nullable: false),
                    week_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    minutes_played = table.Column<int>(type: "integer", nullable: false),
                    minutes_toward_next_dc = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("player_playtime_records_pkey", x => x.id);
                    table.ForeignKey(
                        name: "player_playtime_records_cd_key_fkey",
                        column: x => x.cd_key,
                        principalTable: "players",
                        principalColumn: "cd_key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "dm_playtime_records_cdkey_weekstart_key",
                table: "dm_playtime_records",
                columns: new[] { "cd_key", "week_start" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "player_playtime_records_cdkey_weekstart_key",
                table: "player_playtime_records",
                columns: new[] { "cd_key", "week_start" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dm_playtime_records");

            migrationBuilder.DropTable(
                name: "player_playtime_records");
        }
    }
}
