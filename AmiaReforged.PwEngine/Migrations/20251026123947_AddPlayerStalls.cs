using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerStalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerStalls",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    AreaResRef = table.Column<string>(type: "text", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStalls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStalls_PersistedCharacter_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StallProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    ItemData = table.Column<byte[]>(type: "bytea", nullable: false),
                    ShopId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StallProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StallProducts_PlayerStalls_ShopId",
                        column: x => x.ShopId,
                        principalTable: "PlayerStalls",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStalls_CharacterId",
                table: "PlayerStalls",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_StallProducts_ShopId",
                table: "StallProducts",
                column: "ShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StallProducts");

            migrationBuilder.DropTable(
                name: "PlayerStalls");
        }
    }
}
