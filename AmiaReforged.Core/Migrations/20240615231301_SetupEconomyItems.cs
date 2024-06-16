using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class SetupEconomyItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ValueModifier = table.Column<float>(type: "real", nullable: false),
                    MagicModifier = table.Column<float>(type: "real", nullable: false),
                    DurabilityModifier = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Qualities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ValueModifier = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Qualities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stockpiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stockpiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EconomyItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: true),
                    MaterialId = table.Column<int>(type: "integer", nullable: false),
                    QualityId = table.Column<int>(type: "integer", nullable: false),
                    BaseValue = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EconomyItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EconomyItems_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EconomyItems_Qualities_QualityId",
                        column: x => x.QualityId,
                        principalTable: "Qualities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StockpileId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Settlements_Stockpiles_StockpileId",
                        column: x => x.StockpileId,
                        principalTable: "Stockpiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockpileUser",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockpileId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockpileUser", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockpileUser_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockpileUser_Stockpiles_StockpileId",
                        column: x => x.StockpileId,
                        principalTable: "Stockpiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockpiledItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockpileId = table.Column<long>(type: "bigint", nullable: false),
                    AddedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockpiledItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockpiledItems_Characters_AddedBy",
                        column: x => x.AddedBy,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockpiledItems_EconomyItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "EconomyItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockpiledItems_Stockpiles_StockpileId",
                        column: x => x.StockpileId,
                        principalTable: "Stockpiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EconomyItems_MaterialId",
                table: "EconomyItems",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_EconomyItems_QualityId",
                table: "EconomyItems",
                column: "QualityId");

            migrationBuilder.CreateIndex(
                name: "IX_Settlements_StockpileId",
                table: "Settlements",
                column: "StockpileId");

            migrationBuilder.CreateIndex(
                name: "IX_StockpiledItems_AddedBy",
                table: "StockpiledItems",
                column: "AddedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StockpiledItems_ItemId",
                table: "StockpiledItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockpiledItems_StockpileId",
                table: "StockpiledItems",
                column: "StockpileId");

            migrationBuilder.CreateIndex(
                name: "IX_StockpileUser_CharacterId",
                table: "StockpileUser",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_StockpileUser_StockpileId",
                table: "StockpileUser",
                column: "StockpileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settlements");

            migrationBuilder.DropTable(
                name: "StockpiledItems");

            migrationBuilder.DropTable(
                name: "StockpileUser");

            migrationBuilder.DropTable(
                name: "EconomyItems");

            migrationBuilder.DropTable(
                name: "Stockpiles");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Qualities");
        }
    }
}
