using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NpcShops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ShopkeeperTag = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RestockMinMinutes = table.Column<int>(type: "integer", nullable: false),
                    RestockMaxMinutes = table.Column<int>(type: "integer", nullable: false),
                    NextRestockUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VaultBalance = table.Column<int>(type: "integer", nullable: false),
                    DefinitionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcShops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NpcShopLedgerEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    ProductResRef = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SalePrice = table.Column<int>(type: "integer", nullable: false),
                    BuyerName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    SoldAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcShopLedgerEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcShopLedgerEntries_NpcShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "NpcShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NpcShopProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    ResRef = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    CurrentStock = table.Column<int>(type: "integer", nullable: false),
                    MaxStock = table.Column<int>(type: "integer", nullable: false),
                    RestockAmount = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    LocalVariablesJson = table.Column<string>(type: "jsonb", nullable: true),
                    AppearanceJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcShopProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcShopProducts_NpcShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "NpcShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NpcShopVaultItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShopId = table.Column<long>(type: "bigint", nullable: false),
                    ItemData = table.Column<byte[]>(type: "bytea", nullable: false),
                    StoredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RetrievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcShopVaultItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcShopVaultItems_NpcShops_ShopId",
                        column: x => x.ShopId,
                        principalTable: "NpcShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NpcShopLedgerEntries_ShopId_SoldAt",
                table: "NpcShopLedgerEntries",
                columns: new[] { "ShopId", "SoldAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NpcShopProducts_ShopId_ResRef",
                table: "NpcShopProducts",
                columns: new[] { "ShopId", "ResRef" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NpcShops_Tag",
                table: "NpcShops",
                column: "Tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NpcShopVaultItems_ShopId_StoredAt",
                table: "NpcShopVaultItems",
                columns: new[] { "ShopId", "StoredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NpcShopLedgerEntries");

            migrationBuilder.DropTable(
                name: "NpcShopProducts");

            migrationBuilder.DropTable(
                name: "NpcShopVaultItems");

            migrationBuilder.DropTable(
                name: "NpcShops");
        }
    }
}
