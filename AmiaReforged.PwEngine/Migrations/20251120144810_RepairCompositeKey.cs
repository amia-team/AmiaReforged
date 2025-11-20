using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class RepairCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "item_tag",
                table: "npc_shop_products",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "npc_shop_products_shop_itemtag_idx",
                table: "npc_shop_products",
                columns: new[] { "shop_id", "item_tag" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "npc_shop_products_shop_itemtag_idx",
                table: "npc_shop_products");

            migrationBuilder.DropColumn(
                name: "item_tag",
                table: "npc_shop_products");
        }
    }
}
