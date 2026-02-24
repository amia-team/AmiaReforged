using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AllowMutationEdits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MutationTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    prefix = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    spawn_chance_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutationTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MutationEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    mutation_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    ability_type = table.Column<int>(type: "integer", nullable: true),
                    damage_type = table.Column<int>(type: "integer", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MutationEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MutationEffects_MutationTemplates_mutation_template_id",
                        column: x => x.mutation_template_id,
                        principalTable: "MutationTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MutationEffects_MutationTemplateId",
                table: "MutationEffects",
                column: "mutation_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_MutationTemplates_Prefix",
                table: "MutationTemplates",
                column: "prefix",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MutationEffects");

            migrationBuilder.DropTable(
                name: "MutationTemplates");
        }
    }
}
