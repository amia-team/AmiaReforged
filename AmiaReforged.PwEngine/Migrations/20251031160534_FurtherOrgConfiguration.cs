using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class FurtherOrgConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PersonaIdString",
                table: "Organizations",
                newName: "persona_id");

            migrationBuilder.AddColumn<string>(
                name: "ban_list",
                table: "Organizations",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "inbox",
                table: "Organizations",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "parent_organization_id",
                table: "Organizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "Organizations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_ParentOrganizationId",
                table: "Organizations",
                column: "parent_organization_id");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Type",
                table: "Organizations",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_ParentOrganizationId",
                table: "Organizations");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_Type",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "ban_list",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "inbox",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "parent_organization_id",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "type",
                table: "Organizations");

            migrationBuilder.RenameColumn(
                name: "persona_id",
                table: "Organizations",
                newName: "PersonaIdString");
        }
    }
}
