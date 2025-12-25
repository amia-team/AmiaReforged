using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddFullRebuildFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalFirstName",
                table: "character_rebuilds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalLastName",
                table: "character_rebuilds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PcKeyData",
                table: "character_rebuilds",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoredGold",
                table: "character_rebuilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoredXp",
                table: "character_rebuilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalFirstName",
                table: "character_rebuilds");

            migrationBuilder.DropColumn(
                name: "OriginalLastName",
                table: "character_rebuilds");

            migrationBuilder.DropColumn(
                name: "PcKeyData",
                table: "character_rebuilds");

            migrationBuilder.DropColumn(
                name: "StoredGold",
                table: "character_rebuilds");

            migrationBuilder.DropColumn(
                name: "StoredXp",
                table: "character_rebuilds");
        }
    }
}
