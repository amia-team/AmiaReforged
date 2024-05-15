using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class ManageDmLoginFromModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "dm_logins_pkey",
                table: "dm_logins");

            migrationBuilder.RenameTable(
                name: "dm_logins",
                newName: "DmLogins");

            migrationBuilder.RenameColumn(
                name: "session_start",
                table: "DmLogins",
                newName: "SessionStart");

            migrationBuilder.RenameColumn(
                name: "session_end",
                table: "DmLogins",
                newName: "SessionEnd");

            migrationBuilder.RenameColumn(
                name: "login_name",
                table: "DmLogins",
                newName: "LoginName");

            migrationBuilder.RenameColumn(
                name: "cd_key",
                table: "DmLogins",
                newName: "CdKey");

            migrationBuilder.RenameColumn(
                name: "login_number",
                table: "DmLogins",
                newName: "LoginNumber");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SessionStart",
                table: "DmLogins",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SessionEnd",
                table: "DmLogins",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "LoginName",
                table: "DmLogins",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CdKey",
                table: "DmLogins",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_DmLogins",
                table: "DmLogins",
                column: "LoginNumber");

            migrationBuilder.CreateIndex(
                name: "IX_DmLogins_CdKey",
                table: "DmLogins",
                column: "CdKey");

            migrationBuilder.AddForeignKey(
                name: "FK_DmLogins_Dms_CdKey",
                table: "DmLogins",
                column: "CdKey",
                principalTable: "Dms",
                principalColumn: "CdKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DmLogins_Dms_CdKey",
                table: "DmLogins");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DmLogins",
                table: "DmLogins");

            migrationBuilder.DropIndex(
                name: "IX_DmLogins_CdKey",
                table: "DmLogins");

            migrationBuilder.RenameTable(
                name: "DmLogins",
                newName: "dm_logins");

            migrationBuilder.RenameColumn(
                name: "SessionStart",
                table: "dm_logins",
                newName: "session_start");

            migrationBuilder.RenameColumn(
                name: "SessionEnd",
                table: "dm_logins",
                newName: "session_end");

            migrationBuilder.RenameColumn(
                name: "LoginName",
                table: "dm_logins",
                newName: "login_name");

            migrationBuilder.RenameColumn(
                name: "CdKey",
                table: "dm_logins",
                newName: "cd_key");

            migrationBuilder.RenameColumn(
                name: "LoginNumber",
                table: "dm_logins",
                newName: "login_number");

            migrationBuilder.AlterColumn<DateTime>(
                name: "session_start",
                table: "dm_logins",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "session_end",
                table: "dm_logins",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "login_name",
                table: "dm_logins",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "cd_key",
                table: "dm_logins",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "dm_logins_pkey",
                table: "dm_logins",
                column: "login_number");
        }
    }
}
