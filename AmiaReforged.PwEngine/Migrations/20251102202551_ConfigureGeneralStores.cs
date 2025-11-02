using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureGeneralStores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "npc_shops",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    shopkeeper_tag = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    manual_restock = table.Column<bool>(type: "boolean", nullable: false),
                    manual_pricing = table.Column<bool>(type: "boolean", nullable: false),
                    owner_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_character_id = table.Column<Guid>(type: "uuid", nullable: true),
                    owner_display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    restock_min_minutes = table.Column<int>(type: "integer", nullable: false),
                    restock_max_minutes = table.Column<int>(type: "integer", nullable: false),
                    next_restock_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    vault_balance = table.Column<int>(type: "integer", nullable: false),
                    definition_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_shops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_npc_shops_CoinHouseAccounts_owner_account_id",
                        column: x => x.owner_account_id,
                        principalTable: "CoinHouseAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "npc_shop_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shop_id = table.Column<long>(type: "bigint", nullable: false),
                    buyer_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    buyer_persona = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<int>(type: "integer", nullable: false),
                    total_price = table.Column<int>(type: "integer", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    resref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_shop_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_npc_shop_ledger_entries_npc_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "npc_shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_shop_products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shop_id = table.Column<long>(type: "bigint", nullable: false),
                    resref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    price = table.Column<int>(type: "integer", nullable: false),
                    current_stock = table.Column<int>(type: "integer", nullable: false),
                    max_stock = table.Column<int>(type: "integer", nullable: false),
                    restock_amount = table.Column<int>(type: "integer", nullable: false),
                    is_player_managed = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    locals_json = table.Column<string>(type: "jsonb", nullable: true),
                    appearance_json = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_shop_products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_npc_shop_products_npc_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "npc_shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "npc_shop_vault_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    shop_id = table.Column<long>(type: "bigint", nullable: false),
                    item_data = table.Column<byte[]>(type: "bytea", nullable: false),
                    item_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    resref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    stored_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_npc_shop_vault_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_npc_shop_vault_items_npc_shops_shop_id",
                        column: x => x.shop_id,
                        principalTable: "npc_shops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "npc_shop_ledger_entries_shop_timestamp_idx",
                table: "npc_shop_ledger_entries",
                columns: new[] { "shop_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "npc_shop_products_shop_resref_idx",
                table: "npc_shop_products",
                columns: new[] { "shop_id", "resref" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "npc_shop_vault_items_shop_timestamp_idx",
                table: "npc_shop_vault_items",
                columns: new[] { "shop_id", "stored_at_utc" });

            migrationBuilder.CreateIndex(
                name: "IX_npc_shops_owner_account_id",
                table: "npc_shops",
                column: "owner_account_id");

            migrationBuilder.CreateIndex(
                name: "npc_shops_tag_idx",
                table: "npc_shops",
                column: "tag",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "npc_shop_ledger_entries");

            migrationBuilder.DropTable(
                name: "npc_shop_products");

            migrationBuilder.DropTable(
                name: "npc_shop_vault_items");

            migrationBuilder.DropTable(
                name: "npc_shops");
        }
    }
}
