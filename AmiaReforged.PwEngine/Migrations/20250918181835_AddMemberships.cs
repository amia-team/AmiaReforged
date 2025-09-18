using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterStatistics_WorldCharacters_CharacterId",
                table: "CharacterStatistics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WorldCharacters",
                table: "WorldCharacters");

            migrationBuilder.RenameTable(
                name: "WorldCharacters",
                newName: "PersistedCharacter");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PersistedCharacter",
                table: "PersistedCharacter",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "IndustryMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    IndustryTag = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndustryMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndustryMemberships_PersistedCharacter_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CharacterKnowledge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IndustryTag = table.Column<string>(type: "text", nullable: false),
                    KnowledgeTag = table.Column<string>(type: "text", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterKnowledge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterKnowledge_IndustryMemberships_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "IndustryMemberships",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterKnowledge_PersistedCharacter_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledge_CharacterId",
                table: "CharacterKnowledge",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledge_MembershipId",
                table: "CharacterKnowledge",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_IndustryMemberships_CharacterId",
                table: "IndustryMemberships",
                column: "CharacterId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterStatistics_PersistedCharacter_CharacterId",
                table: "CharacterStatistics",
                column: "CharacterId",
                principalTable: "PersistedCharacter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterStatistics_PersistedCharacter_CharacterId",
                table: "CharacterStatistics");

            migrationBuilder.DropTable(
                name: "CharacterKnowledge");

            migrationBuilder.DropTable(
                name: "IndustryMemberships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PersistedCharacter",
                table: "PersistedCharacter");

            migrationBuilder.RenameTable(
                name: "PersistedCharacter",
                newName: "WorldCharacters");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WorldCharacters",
                table: "WorldCharacters",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterStatistics_WorldCharacters_CharacterId",
                table: "CharacterStatistics",
                column: "CharacterId",
                principalTable: "WorldCharacters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
