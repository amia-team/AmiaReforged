using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterKnowledge_IndustryMemberships_MembershipId",
                table: "CharacterKnowledge");

            migrationBuilder.DropIndex(
                name: "IX_CharacterKnowledge_MembershipId",
                table: "CharacterKnowledge");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "CharacterKnowledge");

            migrationBuilder.AddColumn<Guid>(
                name: "PersistentIndustryMembershipId",
                table: "CharacterKnowledge",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledge_PersistentIndustryMembershipId",
                table: "CharacterKnowledge",
                column: "PersistentIndustryMembershipId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterKnowledge_IndustryMemberships_PersistentIndustryMe~",
                table: "CharacterKnowledge",
                column: "PersistentIndustryMembershipId",
                principalTable: "IndustryMemberships",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CharacterKnowledge_IndustryMemberships_PersistentIndustryMe~",
                table: "CharacterKnowledge");

            migrationBuilder.DropIndex(
                name: "IX_CharacterKnowledge_PersistentIndustryMembershipId",
                table: "CharacterKnowledge");

            migrationBuilder.DropColumn(
                name: "PersistentIndustryMembershipId",
                table: "CharacterKnowledge");

            migrationBuilder.AddColumn<Guid>(
                name: "MembershipId",
                table: "CharacterKnowledge",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_CharacterKnowledge_MembershipId",
                table: "CharacterKnowledge",
                column: "MembershipId");

            migrationBuilder.AddForeignKey(
                name: "FK_CharacterKnowledge_IndustryMemberships_MembershipId",
                table: "CharacterKnowledge",
                column: "MembershipId",
                principalTable: "IndustryMemberships",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
