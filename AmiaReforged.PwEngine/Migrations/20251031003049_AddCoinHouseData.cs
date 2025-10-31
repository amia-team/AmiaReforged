using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddCoinHouseData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Tag",
                table: "CoinHouses",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "PersonaIdString",
                table: "CoinHouses",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_FromPersonaId",
                table: "Transactions",
                column: "FromPersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Timestamp",
                table: "Transactions",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ToPersonaId",
                table: "Transactions",
                column: "ToPersonaId");

            migrationBuilder.CreateIndex(
                name: "IX_CoinHouses_Tag",
                table: "CoinHouses",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CoinHouseAccountHolders_HolderId_AccountId",
                table: "CoinHouseAccountHolders",
                columns: new[] { "HolderId", "AccountId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_FromPersonaId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Timestamp",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_ToPersonaId",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_CoinHouses_Tag",
                table: "CoinHouses");

            migrationBuilder.DropIndex(
                name: "IX_CoinHouseAccountHolders_HolderId_AccountId",
                table: "CoinHouseAccountHolders");

            migrationBuilder.AlterColumn<string>(
                name: "Tag",
                table: "CoinHouses",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "PersonaIdString",
                table: "CoinHouses",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
