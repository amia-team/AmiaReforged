using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class SerializeSpellbooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SavedSpellbooks",
                table: "SavedSpellbooks");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "SavedSpellbooks");

            migrationBuilder.AddColumn<long>(
                name: "BookId",
                table: "SavedSpellbooks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SavedSpellbooks",
                table: "SavedSpellbooks",
                column: "BookId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SavedSpellbooks",
                table: "SavedSpellbooks");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "SavedSpellbooks");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "SavedSpellbooks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_SavedSpellbooks",
                table: "SavedSpellbooks",
                column: "Id");
        }
    }
}
