using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class FixShopIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "npc_shop_products_shop_resref_idx",
                table: "npc_shop_products");

            migrationBuilder.CreateIndex(
                name: "npc_shop_products_shop_resref_idx",
                table: "npc_shop_products",
                columns: new[] { "shop_id", "resref" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "npc_shop_products_shop_resref_idx",
                table: "npc_shop_products");

            migrationBuilder.CreateIndex(
                name: "npc_shop_products_shop_resref_idx",
                table: "npc_shop_products",
                columns: new[] { "shop_id", "resref" },
                unique: true);
        }
    }
}
