using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class StoreQuickslots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Factions",
                table: "Factions");

            migrationBuilder.DropIndex(
                name: "IX_FactionRelations_FactionName_TargetFactionName",
                table: "FactionRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FactionCharacterRelations",
                table: "FactionCharacterRelations");

            migrationBuilder.DropColumn(
                name: "Members",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "FactionName",
                table: "FactionCharacterRelations");

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "Factions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<long>(
                name: "Id",
                table: "FactionCharacterRelations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<long>(
                name: "FactionId",
                table: "FactionCharacterRelations",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Factions",
                table: "Factions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FactionCharacterRelations",
                table: "FactionCharacterRelations",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "PlayerFactionMember",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FactionId = table.Column<long>(type: "bigint", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerFactionMember", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerFactionMember_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerFactionMember_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SavedQuickslots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerCharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Quickslots = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavedQuickslots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavedQuickslots_Characters_PlayerCharacterId",
                        column: x => x.PlayerCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FactionCharacterRelations_CharacterId",
                table: "FactionCharacterRelations",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_FactionCharacterRelations_FactionId",
                table: "FactionCharacterRelations",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFactionMember_CharacterId",
                table: "PlayerFactionMember",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerFactionMember_FactionId",
                table: "PlayerFactionMember",
                column: "FactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SavedQuickslots_PlayerCharacterId",
                table: "SavedQuickslots",
                column: "PlayerCharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_FactionCharacterRelations_Characters_CharacterId",
                table: "FactionCharacterRelations",
                column: "CharacterId",
                principalTable: "Characters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FactionCharacterRelations_Factions_FactionId",
                table: "FactionCharacterRelations",
                column: "FactionId",
                principalTable: "Factions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FactionCharacterRelations_Characters_CharacterId",
                table: "FactionCharacterRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_FactionCharacterRelations_Factions_FactionId",
                table: "FactionCharacterRelations");

            migrationBuilder.DropTable(
                name: "PlayerFactionMember");

            migrationBuilder.DropTable(
                name: "SavedQuickslots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Factions",
                table: "Factions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FactionCharacterRelations",
                table: "FactionCharacterRelations");

            migrationBuilder.DropIndex(
                name: "IX_FactionCharacterRelations_CharacterId",
                table: "FactionCharacterRelations");

            migrationBuilder.DropIndex(
                name: "IX_FactionCharacterRelations_FactionId",
                table: "FactionCharacterRelations");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Factions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "FactionCharacterRelations");

            migrationBuilder.DropColumn(
                name: "FactionId",
                table: "FactionCharacterRelations");

            migrationBuilder.AddColumn<List<Guid>>(
                name: "Members",
                table: "Factions",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "FactionName",
                table: "FactionCharacterRelations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Factions",
                table: "Factions",
                column: "Name");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FactionCharacterRelations",
                table: "FactionCharacterRelations",
                columns: new[] { "CharacterId", "FactionName" });

            migrationBuilder.CreateIndex(
                name: "IX_FactionRelations_FactionName_TargetFactionName",
                table: "FactionRelations",
                columns: new[] { "FactionName", "TargetFactionName" },
                unique: true);
        }
    }
}
