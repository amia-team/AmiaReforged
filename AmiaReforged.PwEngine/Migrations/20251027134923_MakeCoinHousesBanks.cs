using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class MakeCoinHousesBanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CoinHouses_PersistedCharacter_AccountHolderId",
                table: "CoinHouses");

            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStalls_PersistedCharacter_CharacterId",
                table: "PlayerStalls");

            migrationBuilder.DropForeignKey(
                name: "FK_StallTransactions_PersistedCharacter_StallOwnerId",
                table: "StallTransactions");

            migrationBuilder.DropIndex(
                name: "IX_CoinHouses_AccountHolderId",
                table: "CoinHouses");

            migrationBuilder.DropColumn(
                name: "PayRentFromHeldProfits",
                table: "PlayerStalls");

            migrationBuilder.DropColumn(
                name: "AccountHolderId",
                table: "CoinHouses");

            migrationBuilder.RenameColumn(
                name: "CharacterId",
                table: "PlayerStalls",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerStalls_CharacterId",
                table: "PlayerStalls",
                newName: "IX_PlayerStalls_AccountId");

            migrationBuilder.CreateTable(
                name: "CoinHouseAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Debit = table.Column<int>(type: "integer", nullable: false),
                    Credit = table.Column<int>(type: "integer", nullable: false),
                    CoinHouseId = table.Column<long>(type: "bigint", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinHouseAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoinHouseAccounts_CoinHouses_CoinHouseId",
                        column: x => x.CoinHouseId,
                        principalTable: "CoinHouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoinHouseAccountHolders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    LastName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    HolderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinHouseAccountHolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoinHouseAccountHolders_CoinHouseAccounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "CoinHouseAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CoinHouseTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    IssuerId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuerType = table.Column<int>(type: "integer", nullable: false),
                    CoinHouseAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoinHouseTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CoinHouseTransactions_CoinHouseAccounts_CoinHouseAccountId",
                        column: x => x.CoinHouseAccountId,
                        principalTable: "CoinHouseAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CoinHouseAccountHolders_AccountId",
                table: "CoinHouseAccountHolders",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CoinHouseAccounts_CoinHouseId",
                table: "CoinHouseAccounts",
                column: "CoinHouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CoinHouseTransactions_CoinHouseAccountId",
                table: "CoinHouseTransactions",
                column: "CoinHouseAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStalls_CoinHouseAccounts_AccountId",
                table: "PlayerStalls",
                column: "AccountId",
                principalTable: "CoinHouseAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StallTransactions_CoinHouseAccounts_StallOwnerId",
                table: "StallTransactions",
                column: "StallOwnerId",
                principalTable: "CoinHouseAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStalls_CoinHouseAccounts_AccountId",
                table: "PlayerStalls");

            migrationBuilder.DropForeignKey(
                name: "FK_StallTransactions_CoinHouseAccounts_StallOwnerId",
                table: "StallTransactions");

            migrationBuilder.DropTable(
                name: "CoinHouseAccountHolders");

            migrationBuilder.DropTable(
                name: "CoinHouseTransactions");

            migrationBuilder.DropTable(
                name: "CoinHouseAccounts");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "PlayerStalls",
                newName: "CharacterId");

            migrationBuilder.RenameIndex(
                name: "IX_PlayerStalls_AccountId",
                table: "PlayerStalls",
                newName: "IX_PlayerStalls_CharacterId");

            migrationBuilder.AddColumn<bool>(
                name: "PayRentFromHeldProfits",
                table: "PlayerStalls",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "AccountHolderId",
                table: "CoinHouses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoinHouses_AccountHolderId",
                table: "CoinHouses",
                column: "AccountHolderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CoinHouses_PersistedCharacter_AccountHolderId",
                table: "CoinHouses",
                column: "AccountHolderId",
                principalTable: "PersistedCharacter",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStalls_PersistedCharacter_CharacterId",
                table: "PlayerStalls",
                column: "CharacterId",
                principalTable: "PersistedCharacter",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StallTransactions_PersistedCharacter_StallOwnerId",
                table: "StallTransactions",
                column: "StallOwnerId",
                principalTable: "PersistedCharacter",
                principalColumn: "Id");
        }
    }
}
