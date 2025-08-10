using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations.Economy
{
    /// <inheritdoc />
    public partial class Economy_FixNodeInstanceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExistingNodes");

            migrationBuilder.CreateTable(
                name: "NodeInstances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DefinitionId = table.Column<string>(type: "text", nullable: false),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    Richness = table.Column<float>(type: "real", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Scale = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NodeInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NodeInstances_NodeDefinitions_DefinitionId",
                        column: x => x.DefinitionId,
                        principalTable: "NodeDefinitions",
                        principalColumn: "Tag",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NodeInstances_SavedLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "SavedLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstances_DefinitionId",
                table: "NodeInstances",
                column: "DefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_NodeInstances_LocationId",
                table: "NodeInstances",
                column: "LocationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NodeInstances");

            migrationBuilder.CreateTable(
                name: "ExistingNodes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LocationId = table.Column<long>(type: "bigint", nullable: false),
                    ResourceTag = table.Column<string>(type: "text", nullable: false),
                    Richness = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExistingNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExistingNodes_SavedLocations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "SavedLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExistingNodes_LocationId",
                table: "ExistingNodes",
                column: "LocationId");
        }
    }
}
