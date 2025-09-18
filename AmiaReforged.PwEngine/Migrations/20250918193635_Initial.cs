using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersistedCharacter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistedCharacter", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PersistedNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Area = table.Column<string>(type: "text", nullable: false),
                    DefinitionTag = table.Column<string>(type: "text", nullable: false),
                    Uses = table.Column<int>(type: "integer", nullable: false),
                    Quality = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    Z = table.Column<float>(type: "real", nullable: false),
                    Rotation = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersistedNodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorldConfiguration",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    ValueType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorldConfiguration", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "CharacterStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgePoints = table.Column<int>(type: "integer", nullable: false),
                    TimesDied = table.Column<int>(type: "integer", nullable: false),
                    TimesRankedUp = table.Column<int>(type: "integer", nullable: false),
                    IndustriesJoined = table.Column<int>(type: "integer", nullable: false),
                    PlayTime = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterStatistics_PersistedCharacter_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    PersistentIndustryMembershipId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterKnowledge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterKnowledge_IndustryMemberships_PersistentIndustryMe~",
                        column: x => x.PersistentIndustryMembershipId,
                        principalTable: "IndustryMemberships",
                        principalColumn: "Id");
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
                name: "IX_CharacterKnowledge_PersistentIndustryMembershipId",
                table: "CharacterKnowledge",
                column: "PersistentIndustryMembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterStatistics_CharacterId",
                table: "CharacterStatistics",
                column: "CharacterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndustryMemberships_CharacterId",
                table: "IndustryMemberships",
                column: "CharacterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterKnowledge");

            migrationBuilder.DropTable(
                name: "CharacterStatistics");

            migrationBuilder.DropTable(
                name: "PersistedNodes");

            migrationBuilder.DropTable(
                name: "WorldConfiguration");

            migrationBuilder.DropTable(
                name: "IndustryMemberships");

            migrationBuilder.DropTable(
                name: "PersistedCharacter");
        }
    }
}
