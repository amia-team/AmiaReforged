using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class TweakShopConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "npc_shop_products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "display_name",
                table: "npc_shop_products",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description",
                table: "npc_shop_products");

            migrationBuilder.DropColumn(
                name: "display_name",
                table: "npc_shop_products");
        }
    }
}
