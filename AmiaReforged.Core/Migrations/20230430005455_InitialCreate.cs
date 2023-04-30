using System;
using System.Collections.Generic;
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
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CdKey = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    IsPlayerCharacter = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
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
                name: "FactionCharacterRelations",
                columns: table => new
                {
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionName = table.Column<string>(type: "text", nullable: false),
                    Relation = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionCharacterRelations", x => new { x.CharacterId, x.FactionName });
                });

            migrationBuilder.CreateTable(
                name: "FactionRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FactionName = table.Column<string>(type: "text", nullable: false),
                    TargetFactionName = table.Column<string>(type: "text", nullable: false),
                    Relation = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FactionRelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Members = table.Column<List<Guid>>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Name);
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

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelations_FactionName_TargetFactionName",
                table: "FactionRelations",
                columns: new[] { "FactionName", "TargetFactionName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bans");

            migrationBuilder.DropTable(
                name: "Characters");

            migrationBuilder.DropTable(
                name: "dm_logins");

            migrationBuilder.DropTable(
                name: "dms");

            migrationBuilder.DropTable(
                name: "dreamcoin_records");

            migrationBuilder.DropTable(
                name: "FactionCharacterRelations");

            migrationBuilder.DropTable(
                name: "FactionRelations");

            migrationBuilder.DropTable(
                name: "Factions");

            migrationBuilder.DropTable(
                name: "players");
        }
    }
}
