using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRebuildForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_character_rebuilds_PersistedCharacter_character_id",
                table: "character_rebuilds");

            migrationBuilder.DropForeignKey(
                name: "FK_character_rebuilds_player_personas_player_cd_key",
                table: "character_rebuilds");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_character_rebuilds_PersistedCharacter_character_id",
                table: "character_rebuilds",
                column: "character_id",
                principalTable: "PersistedCharacter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_character_rebuilds_player_personas_player_cd_key",
                table: "character_rebuilds",
                column: "player_cd_key",
                principalTable: "player_personas",
                principalColumn: "cd_key",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
