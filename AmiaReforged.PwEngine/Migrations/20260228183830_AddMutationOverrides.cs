using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddMutationOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "override_mutations",
                table: "SpawnGroups",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "GroupMutationOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mutation_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chance_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 10)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMutationOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMutationOverrides_MutationTemplates_mutation_template_~",
                        column: x => x.mutation_template_id,
                        principalTable: "MutationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GroupMutationOverrides_SpawnGroups_spawn_group_id",
                        column: x => x.spawn_group_id,
                        principalTable: "SpawnGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupMutationOverrides_Group_Mutation",
                table: "GroupMutationOverrides",
                columns: new[] { "spawn_group_id", "mutation_template_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GroupMutationOverrides_MutationTemplateId",
                table: "GroupMutationOverrides",
                column: "mutation_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_GroupMutationOverrides_SpawnGroupId",
                table: "GroupMutationOverrides",
                column: "spawn_group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupMutationOverrides");

            migrationBuilder.DropColumn(
                name: "override_mutations",
                table: "SpawnGroups");
        }
    }
}
