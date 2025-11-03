using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class FactorInSelling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accepted_base_item_types",
                table: "npc_shops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "markup_percent",
                table: "npc_shops",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "base_item_type",
                table: "npc_shop_products",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accepted_base_item_types",
                table: "npc_shops");

            migrationBuilder.DropColumn(
                name: "markup_percent",
                table: "npc_shops");

            migrationBuilder.DropColumn(
                name: "base_item_type",
                table: "npc_shop_products");
        }
    }
}
