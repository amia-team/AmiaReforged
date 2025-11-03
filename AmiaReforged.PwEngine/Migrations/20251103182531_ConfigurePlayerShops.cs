using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurePlayerShops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerStalls_CoinHouseAccounts_AccountId",
                table: "PlayerStalls");

            migrationBuilder.DropForeignKey(
                name: "FK_StallProducts_PlayerStalls_ShopId",
                table: "StallProducts");

            migrationBuilder.DropForeignKey(
                name: "FK_StallTransactions_CoinHouseAccounts_StallOwnerId",
                table: "StallTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_StallTransactions_PlayerStalls_StallId",
                table: "StallTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StallTransactions",
                table: "StallTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StallTransactions_StallId",
                table: "StallTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StallTransactions_StallOwnerId",
                table: "StallTransactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_StallProducts",
                table: "StallProducts");

            migrationBuilder.DropIndex(
                name: "IX_StallProducts_ShopId",
                table: "StallProducts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlayerStalls",
                table: "PlayerStalls");

            migrationBuilder.DropIndex(
                name: "IX_PlayerStalls_AccountId",
                table: "PlayerStalls");

            migrationBuilder.DropColumn(
                name: "BuyerName",
                table: "StallTransactions");

            migrationBuilder.DropColumn(
                name: "PurchasedAt",
                table: "StallTransactions");

            migrationBuilder.DropColumn(
                name: "StallOwnerId",
                table: "StallTransactions");

            migrationBuilder.DropColumn(
                name: "AddedAt",
                table: "StallProducts");

            migrationBuilder.DropColumn(
                name: "ShopId",
                table: "StallProducts");

            migrationBuilder.RenameTable(
                name: "StallTransactions",
                newName: "player_stall_transactions");

            migrationBuilder.RenameTable(
                name: "StallProducts",
                newName: "player_stall_products");

            migrationBuilder.RenameTable(
                name: "PlayerStalls",
                newName: "player_stalls");

            migrationBuilder.RenameColumn(
                name: "StallId",
                table: "player_stall_transactions",
                newName: "stall_id");

            migrationBuilder.RenameColumn(
                name: "PricePaid",
                table: "player_stall_transactions",
                newName: "gross_amount");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "player_stall_products",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "player_stall_products",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "player_stall_products",
                newName: "price_per_unit");

            migrationBuilder.RenameColumn(
                name: "ItemData",
                table: "player_stall_products",
                newName: "item_data");

            migrationBuilder.RenameColumn(
                name: "Tag",
                table: "player_stalls",
                newName: "stall_tag");

            migrationBuilder.RenameColumn(
                name: "AreaResRef",
                table: "player_stalls",
                newName: "area_resref");

            migrationBuilder.RenameColumn(
                name: "StoredGold",
                table: "player_stalls",
                newName: "lifetime_net_earnings");

            migrationBuilder.RenameColumn(
                name: "LastPaidRentAt",
                table: "player_stalls",
                newName: "suspended_utc");

            migrationBuilder.RenameColumn(
                name: "GrossProfit",
                table: "player_stalls",
                newName: "lifetime_gross_sales");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "player_stalls",
                newName: "next_rent_due_utc");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "player_stalls",
                newName: "owner_character_id");

            migrationBuilder.AddColumn<string>(
                name: "buyer_display_name",
                table: "player_stall_transactions",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "buyer_persona_id",
                table: "player_stall_transactions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "coinhouse_transaction_id",
                table: "player_stall_transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "deposit_amount",
                table: "player_stall_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "escrow_amount",
                table: "player_stall_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "fee_amount",
                table: "player_stall_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "player_stall_transactions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "occurred_at_utc",
                table: "player_stall_transactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "player_stall_transactions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<long>(
                name: "stall_product_id",
                table: "player_stall_transactions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "player_stall_products",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "description",
                table: "player_stall_products",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "base_item_type",
                table: "player_stall_products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "consigned_by_display_name",
                table: "player_stall_products",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "consigned_by_persona_id",
                table: "player_stall_products",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "player_stall_products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "listed_utc",
                table: "player_stall_products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "player_stall_products",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "quantity",
                table: "player_stall_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "resref",
                table: "player_stall_products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "sold_out_utc",
                table: "player_stall_products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "player_stall_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "stall_id",
                table: "player_stall_products",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_utc",
                table: "player_stall_products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "area_resref",
                table: "player_stalls",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<Guid>(
                name: "coinhouse_account_id",
                table: "player_stalls",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_utc",
                table: "player_stalls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<int>(
                name: "daily_rent",
                table: "player_stalls",
                type: "integer",
                nullable: false,
                defaultValue: 10000);

            migrationBuilder.AddColumn<DateTime>(
                name: "deactivated_utc",
                table: "player_stalls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "escrow_balance",
                table: "player_stalls",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "hold_earnings_in_stall",
                table: "player_stalls",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "player_stalls",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_rent_paid_utc",
                table: "player_stalls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "lease_start_utc",
                table: "player_stalls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "owner_display_name",
                table: "player_stalls",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "owner_persona_id",
                table: "player_stalls",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "settlement_tag",
                table: "player_stalls",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_utc",
                table: "player_stalls",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_stall_transactions",
                table: "player_stall_transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_stall_products",
                table: "player_stall_products",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_player_stalls",
                table: "player_stalls",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "player_stall_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StallId = table.Column<long>(type: "bigint", nullable: false),
                    entry_type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<int>(type: "integer", nullable: false),
                    currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, defaultValue: "gp"),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    stall_transaction_id = table.Column<long>(type: "bigint", nullable: true),
                    occurred_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    metadata_json = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_stall_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_stall_ledger_entries_player_stall_transactions_stall~",
                        column: x => x.stall_transaction_id,
                        principalTable: "player_stall_transactions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_player_stall_ledger_entries_player_stalls_StallId",
                        column: x => x.StallId,
                        principalTable: "player_stalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_stall_members",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StallId = table.Column<long>(type: "bigint", nullable: false),
                    persona_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    can_manage_inventory = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_configure_settings = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_collect_earnings = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    added_by_persona_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    added_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    revoked_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_stall_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_player_stall_members_player_stalls_StallId",
                        column: x => x.StallId,
                        principalTable: "player_stalls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_stall_transactions_coinhouse_transaction_id",
                table: "player_stall_transactions",
                column: "coinhouse_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_player_stall_transactions_stall_product_id",
                table: "player_stall_transactions",
                column: "stall_product_id");

            migrationBuilder.CreateIndex(
                name: "player_stall_transactions_idx",
                table: "player_stall_transactions",
                columns: new[] { "stall_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "player_stall_products_active_idx",
                table: "player_stall_products",
                columns: new[] { "stall_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "IX_player_stalls_coinhouse_account_id",
                table: "player_stalls",
                column: "coinhouse_account_id");

            migrationBuilder.CreateIndex(
                name: "player_stalls_owner_area_idx",
                table: "player_stalls",
                columns: new[] { "owner_persona_id", "area_resref" },
                unique: true,
                filter: "owner_persona_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "player_stalls_tag_idx",
                table: "player_stalls",
                column: "stall_tag",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_stall_ledger_entries_stall_transaction_id",
                table: "player_stall_ledger_entries",
                column: "stall_transaction_id");

            migrationBuilder.CreateIndex(
                name: "player_stall_ledger_entries_idx",
                table: "player_stall_ledger_entries",
                columns: new[] { "StallId", "occurred_utc" });

            migrationBuilder.CreateIndex(
                name: "player_stall_members_unique_idx",
                table: "player_stall_members",
                columns: new[] { "StallId", "persona_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_player_stall_products_player_stalls_stall_id",
                table: "player_stall_products",
                column: "stall_id",
                principalTable: "player_stalls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_player_stall_transactions_CoinHouseTransactions_coinhouse_t~",
                table: "player_stall_transactions",
                column: "coinhouse_transaction_id",
                principalTable: "CoinHouseTransactions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_player_stall_transactions_player_stall_products_stall_produ~",
                table: "player_stall_transactions",
                column: "stall_product_id",
                principalTable: "player_stall_products",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_player_stall_transactions_player_stalls_stall_id",
                table: "player_stall_transactions",
                column: "stall_id",
                principalTable: "player_stalls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_player_stalls_CoinHouseAccounts_coinhouse_account_id",
                table: "player_stalls",
                column: "coinhouse_account_id",
                principalTable: "CoinHouseAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_player_stall_products_player_stalls_stall_id",
                table: "player_stall_products");

            migrationBuilder.DropForeignKey(
                name: "FK_player_stall_transactions_CoinHouseTransactions_coinhouse_t~",
                table: "player_stall_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_player_stall_transactions_player_stall_products_stall_produ~",
                table: "player_stall_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_player_stall_transactions_player_stalls_stall_id",
                table: "player_stall_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_player_stalls_CoinHouseAccounts_coinhouse_account_id",
                table: "player_stalls");

            migrationBuilder.DropTable(
                name: "player_stall_ledger_entries");

            migrationBuilder.DropTable(
                name: "player_stall_members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_stalls",
                table: "player_stalls");

            migrationBuilder.DropIndex(
                name: "IX_player_stalls_coinhouse_account_id",
                table: "player_stalls");

            migrationBuilder.DropIndex(
                name: "player_stalls_owner_area_idx",
                table: "player_stalls");

            migrationBuilder.DropIndex(
                name: "player_stalls_tag_idx",
                table: "player_stalls");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_stall_transactions",
                table: "player_stall_transactions");

            migrationBuilder.DropIndex(
                name: "IX_player_stall_transactions_coinhouse_transaction_id",
                table: "player_stall_transactions");

            migrationBuilder.DropIndex(
                name: "IX_player_stall_transactions_stall_product_id",
                table: "player_stall_transactions");

            migrationBuilder.DropIndex(
                name: "player_stall_transactions_idx",
                table: "player_stall_transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_player_stall_products",
                table: "player_stall_products");

            migrationBuilder.DropIndex(
                name: "player_stall_products_active_idx",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "coinhouse_account_id",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "created_utc",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "daily_rent",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "deactivated_utc",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "escrow_balance",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "hold_earnings_in_stall",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "last_rent_paid_utc",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "lease_start_utc",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "owner_display_name",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "owner_persona_id",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "settlement_tag",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "updated_utc",
                table: "player_stalls");

            migrationBuilder.DropColumn(
                name: "buyer_display_name",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "buyer_persona_id",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "coinhouse_transaction_id",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "deposit_amount",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "escrow_amount",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "fee_amount",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "occurred_at_utc",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "stall_product_id",
                table: "player_stall_transactions");

            migrationBuilder.DropColumn(
                name: "base_item_type",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "consigned_by_display_name",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "consigned_by_persona_id",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "listed_utc",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "quantity",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "resref",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "sold_out_utc",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "stall_id",
                table: "player_stall_products");

            migrationBuilder.DropColumn(
                name: "updated_utc",
                table: "player_stall_products");

            migrationBuilder.RenameTable(
                name: "player_stalls",
                newName: "PlayerStalls");

            migrationBuilder.RenameTable(
                name: "player_stall_transactions",
                newName: "StallTransactions");

            migrationBuilder.RenameTable(
                name: "player_stall_products",
                newName: "StallProducts");

            migrationBuilder.RenameColumn(
                name: "stall_tag",
                table: "PlayerStalls",
                newName: "Tag");

            migrationBuilder.RenameColumn(
                name: "area_resref",
                table: "PlayerStalls",
                newName: "AreaResRef");

            migrationBuilder.RenameColumn(
                name: "suspended_utc",
                table: "PlayerStalls",
                newName: "LastPaidRentAt");

            migrationBuilder.RenameColumn(
                name: "owner_character_id",
                table: "PlayerStalls",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "next_rent_due_utc",
                table: "PlayerStalls",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "lifetime_net_earnings",
                table: "PlayerStalls",
                newName: "StoredGold");

            migrationBuilder.RenameColumn(
                name: "lifetime_gross_sales",
                table: "PlayerStalls",
                newName: "GrossProfit");

            migrationBuilder.RenameColumn(
                name: "stall_id",
                table: "StallTransactions",
                newName: "StallId");

            migrationBuilder.RenameColumn(
                name: "gross_amount",
                table: "StallTransactions",
                newName: "PricePaid");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "StallProducts",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "description",
                table: "StallProducts",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "price_per_unit",
                table: "StallProducts",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "item_data",
                table: "StallProducts",
                newName: "ItemData");

            migrationBuilder.AlterColumn<string>(
                name: "AreaResRef",
                table: "PlayerStalls",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AddColumn<string>(
                name: "BuyerName",
                table: "StallTransactions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchasedAt",
                table: "StallTransactions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "StallOwnerId",
                table: "StallTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StallProducts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "StallProducts",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AddedAt",
                table: "StallProducts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "ShopId",
                table: "StallProducts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlayerStalls",
                table: "PlayerStalls",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StallTransactions",
                table: "StallTransactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StallProducts",
                table: "StallProducts",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStalls_AccountId",
                table: "PlayerStalls",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_StallTransactions_StallId",
                table: "StallTransactions",
                column: "StallId");

            migrationBuilder.CreateIndex(
                name: "IX_StallTransactions_StallOwnerId",
                table: "StallTransactions",
                column: "StallOwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_StallProducts_ShopId",
                table: "StallProducts",
                column: "ShopId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerStalls_CoinHouseAccounts_AccountId",
                table: "PlayerStalls",
                column: "AccountId",
                principalTable: "CoinHouseAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StallProducts_PlayerStalls_ShopId",
                table: "StallProducts",
                column: "ShopId",
                principalTable: "PlayerStalls",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StallTransactions_CoinHouseAccounts_StallOwnerId",
                table: "StallTransactions",
                column: "StallOwnerId",
                principalTable: "CoinHouseAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StallTransactions_PlayerStalls_StallId",
                table: "StallTransactions",
                column: "StallId",
                principalTable: "PlayerStalls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
