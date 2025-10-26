using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class MakeCharacterNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Houses_PersistedCharacter_CharacterId",
                table: "Houses");

            migrationBuilder.AlterColumn<Guid>(
                name: "CharacterId",
                table: "Houses",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_PersistedCharacter_CharacterId",
                table: "Houses",
                column: "CharacterId",
                principalTable: "PersistedCharacter",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Houses_PersistedCharacter_CharacterId",
                table: "Houses");

            migrationBuilder.AlterColumn<Guid>(
                name: "CharacterId",
                table: "Houses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Houses_PersistedCharacter_CharacterId",
                table: "Houses",
                column: "CharacterId",
                principalTable: "PersistedCharacter",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
