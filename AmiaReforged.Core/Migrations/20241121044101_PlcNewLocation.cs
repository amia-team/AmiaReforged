using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class PlcNewLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Area",
                table: "PersistPLC");

            migrationBuilder.DropColumn(
                name: "PLC",
                table: "PersistPLC");

            migrationBuilder.DropColumn(
                name: "Area",
                table: "LastLocation");

            migrationBuilder.AddColumn<string>(
                name: "AreaResRef",
                table: "PersistPLC",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PLCDescription",
                table: "PersistPLC",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PLCName",
                table: "PersistPLC",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PLCResRef",
                table: "PersistPLC",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AreaResRef",
                table: "LastLocation",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AreaResRef",
                table: "PersistPLC");

            migrationBuilder.DropColumn(
                name: "PLCDescription",
                table: "PersistPLC");

            migrationBuilder.DropColumn(
                name: "PLCName",
                table: "PersistPLC");

            migrationBuilder.DropColumn(
                name: "PLCResRef",
                table: "PersistPLC");

            migrationBuilder.DropColumn(
                name: "AreaResRef",
                table: "LastLocation");

            migrationBuilder.AddColumn<long>(
                name: "Area",
                table: "PersistPLC",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "PLC",
                table: "PersistPLC",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Area",
                table: "LastLocation",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
