using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class SerializeStoredItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerItems",
                table: "PlayerItems");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PlayerItems");

            migrationBuilder.AddColumn<long>(
                name: "ItemId",
                table: "PlayerItems",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerItems",
                table: "PlayerItems",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerItems",
                table: "PlayerItems");

            migrationBuilder.DropColumn(
                name: "ItemId",
                table: "PlayerItems");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "PlayerItems",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerItems",
                table: "PlayerItems",
                column: "Id");
        }
    }
}
