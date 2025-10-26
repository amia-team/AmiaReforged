using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddProfitAndTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AddedAt",
                table: "StallProducts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "GrossProfit",
                table: "PlayerStalls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoredGold",
                table: "PlayerStalls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StallTransactions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BuyerName = table.Column<string>(type: "text", nullable: true),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PricePaid = table.Column<int>(type: "integer", nullable: false),
                    StallId = table.Column<long>(type: "bigint", nullable: false),
                    StallOwnerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StallTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StallTransactions_PersistedCharacter_StallOwnerId",
                        column: x => x.StallOwnerId,
                        principalTable: "PersistedCharacter",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StallTransactions_PlayerStalls_StallId",
                        column: x => x.StallId,
                        principalTable: "PlayerStalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StallTransactions_StallId",
                table: "StallTransactions",
                column: "StallId");

            migrationBuilder.CreateIndex(
                name: "IX_StallTransactions_StallOwnerId",
                table: "StallTransactions",
                column: "StallOwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StallTransactions");

            migrationBuilder.DropColumn(
                name: "AddedAt",
                table: "StallProducts");

            migrationBuilder.DropColumn(
                name: "GrossProfit",
                table: "PlayerStalls");

            migrationBuilder.DropColumn(
                name: "StoredGold",
                table: "PlayerStalls");
        }
    }
}
