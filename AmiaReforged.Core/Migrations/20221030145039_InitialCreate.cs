using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bans",
                columns: table => new
                {
                    cd_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bans", x => x.cd_key);
                });

            migrationBuilder.CreateTable(
                name: "characters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cd_key = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("character_id", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dm_logins",
                columns: table => new
                {
                    login_number = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cd_key = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    login_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    session_start = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    session_end = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("dm_logins_pkey", x => x.login_number);
                });

            migrationBuilder.CreateTable(
                name: "dms",
                columns: table => new
                {
                    cd_key = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    cd_key = table.Column<string>(type: "character varying", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("players_pkey", x => x.cd_key);
                });

            migrationBuilder.CreateTable(
                name: "dreamcoin_records",
                columns: table => new
                {
                    cd_key = table.Column<string>(type: "character varying", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("dreamcoin_records_pkey", x => x.cd_key);
                    table.ForeignKey(
                        name: "dreamcoin_records_cd_key_fkey",
                        column: x => x.cd_key,
                        principalTable: "players",
                        principalColumn: "cd_key");
                });

            migrationBuilder.CreateIndex(
                name: "bans_cd_key_key",
                table: "bans",
                column: "cd_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "dms_cd_key_key",
                table: "dms",
                column: "cd_key",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bans");

            migrationBuilder.DropTable(
                name: "characters");

            migrationBuilder.DropTable(
                name: "dm_logins");

            migrationBuilder.DropTable(
                name: "dms");

            migrationBuilder.DropTable(
                name: "dreamcoin_records");

            migrationBuilder.DropTable(
                name: "players");
        }
    }
}
