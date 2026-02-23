using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddSpawnpresets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpawnProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    area_resref = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cooldown_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 900),
                    despawn_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 600),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MiniBossConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creature_resref = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    spawn_chance_percent = table.Column<int>(type: "integer", nullable: false, defaultValue: 5)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MiniBossConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MiniBossConfigs_SpawnProfiles_spawn_profile_id",
                        column: x => x.spawn_profile_id,
                        principalTable: "SpawnProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpawnGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnGroups_SpawnProfiles_spawn_profile_id",
                        column: x => x.spawn_profile_id,
                        principalTable: "SpawnProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpawnBonuses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_profile_id = table.Column<Guid>(type: "uuid", nullable: true),
                    mini_boss_config_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<int>(type: "integer", nullable: false),
                    duration_seconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnBonuses_MiniBossConfigs_mini_boss_config_id",
                        column: x => x.mini_boss_config_id,
                        principalTable: "MiniBossConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpawnBonuses_SpawnProfiles_spawn_profile_id",
                        column: x => x.spawn_profile_id,
                        principalTable: "SpawnProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpawnConditions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    @operator = table.Column<string>(name: "operator", type: "character varying(16)", maxLength: 16, nullable: false),
                    value = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnConditions_SpawnGroups_spawn_group_id",
                        column: x => x.spawn_group_id,
                        principalTable: "SpawnGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpawnEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    spawn_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    creature_resref = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    relative_weight = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    min_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    max_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 4)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpawnEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpawnEntries_SpawnGroups_spawn_group_id",
                        column: x => x.spawn_group_id,
                        principalTable: "SpawnGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MiniBossConfigs_SpawnProfileId",
                table: "MiniBossConfigs",
                column: "spawn_profile_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpawnBonuses_MiniBossConfigId",
                table: "SpawnBonuses",
                column: "mini_boss_config_id");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnBonuses_SpawnProfileId",
                table: "SpawnBonuses",
                column: "spawn_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnConditions_SpawnGroupId",
                table: "SpawnConditions",
                column: "spawn_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnEntries_SpawnGroupId",
                table: "SpawnEntries",
                column: "spawn_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnGroups_SpawnProfileId",
                table: "SpawnGroups",
                column: "spawn_profile_id");

            migrationBuilder.CreateIndex(
                name: "IX_SpawnProfiles_AreaResRef",
                table: "SpawnProfiles",
                column: "area_resref",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpawnBonuses");

            migrationBuilder.DropTable(
                name: "SpawnConditions");

            migrationBuilder.DropTable(
                name: "SpawnEntries");

            migrationBuilder.DropTable(
                name: "MiniBossConfigs");

            migrationBuilder.DropTable(
                name: "SpawnGroups");

            migrationBuilder.DropTable(
                name: "SpawnProfiles");
        }
    }
}
