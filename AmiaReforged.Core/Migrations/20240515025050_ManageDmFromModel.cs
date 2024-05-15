using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class ManageDmFromModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "dms_cd_key_key",
                table: "dms");

            migrationBuilder.RenameTable(
                name: "dms",
                newName: "Dms");

            migrationBuilder.RenameColumn(
                name: "cd_key",
                table: "Dms",
                newName: "CdKey");

            migrationBuilder.AddColumn<string>(
                name: "LoginName",
                table: "Dms",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Dms",
                table: "Dms",
                column: "CdKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Dms",
                table: "Dms");

            migrationBuilder.DropColumn(
                name: "LoginName",
                table: "Dms");

            migrationBuilder.RenameTable(
                name: "Dms",
                newName: "dms");

            migrationBuilder.RenameColumn(
                name: "CdKey",
                table: "dms",
                newName: "cd_key");

            migrationBuilder.CreateIndex(
                name: "dms_cd_key_key",
                table: "dms",
                column: "cd_key",
                unique: true);
        }
    }
}
