using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class UsePersonaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PersonaIdString",
                table: "PersistedCharacter",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonaIdString",
                table: "CoinHouses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonaIdString",
                table: "PersistedCharacter");

            migrationBuilder.DropColumn(
                name: "PersonaIdString",
                table: "CoinHouses");
        }
    }
}
