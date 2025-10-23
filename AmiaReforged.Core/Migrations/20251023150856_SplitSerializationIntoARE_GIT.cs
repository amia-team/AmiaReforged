using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class SplitSerializationIntoARE_GIT : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SerializedCopy",
                table: "DmAreas",
                newName: "SerializedGIT");

            migrationBuilder.AddColumn<byte[]>(
                name: "SerializedARE",
                table: "DmAreas",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SerializedARE",
                table: "DmAreas");

            migrationBuilder.RenameColumn(
                name: "SerializedGIT",
                table: "DmAreas",
                newName: "SerializedCopy");
        }
    }
}
