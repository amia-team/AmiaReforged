using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ExtendPlayerStalls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentTenureGrossSales",
                table: "player_stalls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CurrentTenureNetEarnings",
                table: "player_stalls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerCharacterId",
                table: "player_stall_ledger_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerPersonaId",
                table: "player_stall_ledger_entries",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentTenureGrossSales",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "CurrentTenureNetEarnings",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "OwnerCharacterId",
                table: "player_stall_ledger_entries");

            migrationBuilder.DropColumn(
                name: "OwnerPersonaId",
                table: "player_stall_ledger_entries");
        }
    }
}
