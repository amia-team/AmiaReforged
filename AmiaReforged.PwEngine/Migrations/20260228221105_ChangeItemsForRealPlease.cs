using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ChangeItemsForRealPlease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemBlueprints",
                table: "ItemBlueprints");

            migrationBuilder.DropIndex(
                name: "IX_ItemBlueprints_ItemTag",
                table: "ItemBlueprints");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemBlueprints",
                table: "ItemBlueprints",
                column: "item_tag");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBlueprints_ResRef",
                table: "ItemBlueprints",
                column: "res_ref");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemBlueprints",
                table: "ItemBlueprints");

            migrationBuilder.DropIndex(
                name: "IX_ItemBlueprints_ResRef",
                table: "ItemBlueprints");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemBlueprints",
                table: "ItemBlueprints",
                column: "res_ref");

            migrationBuilder.CreateIndex(
                name: "IX_ItemBlueprints_ItemTag",
                table: "ItemBlueprints",
                column: "item_tag",
                unique: true);
        }
    }
}
