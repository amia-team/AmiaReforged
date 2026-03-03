using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddBossConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "boss_spawn_chance_percent",
                table: "SpawnProfiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "boss_config_id",
                table: "SpawnBonuses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BossConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creature_resref = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BossConfigs_SpawnProfiles_spawn_profile_id",
                        column: x => x.spawn_profile_id,
                        principalTable: "SpawnProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BossConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    boss_config_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(16)", maxLength: 16, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BossConditions_BossConfigs_boss_config_id",
                        column: x => x.boss_config_id,
                        principalTable: "BossConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpawnBonuses_BossConfigId",
                table: "SpawnBonuses",
                column: "boss_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_BossConditions_BossConfigId",
                table: "BossConditions",
                column: "boss_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_BossConfigs_SpawnProfileId",
                table: "BossConfigs",
                column: "spawn_profile_id");

            migrationBuilder.AddForeignKey(
                name: "FK_SpawnBonuses_BossConfigs_boss_config_id",
                table: "SpawnBonuses",
                column: "boss_config_id",
                principalTable: "BossConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpawnBonuses_BossConfigs_boss_config_id",
                table: "SpawnBonuses");

            migrationBuilder.DropTable(
                name: "BossConditions");

            migrationBuilder.DropTable(
                name: "BossConfigs");

            migrationBuilder.DropIndex(
                name: "IX_SpawnBonuses_BossConfigId",
                table: "SpawnBonuses");

            migrationBuilder.DropColumn(
                name: "boss_spawn_chance_percent",
                table: "SpawnProfiles");

            migrationBuilder.DropColumn(
                name: "boss_config_id",
                table: "SpawnBonuses");
        }
    }
}
