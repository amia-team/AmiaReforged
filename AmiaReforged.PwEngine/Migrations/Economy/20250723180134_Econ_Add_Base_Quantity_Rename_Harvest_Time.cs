using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class Econ_Add_Base_Quantity_Rename_Harvest_Time : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HarvestTime",
                table: "NodeDefinitions",
                newName: "BaseQuantity");

            migrationBuilder.AddColumn<int>(
                name: "BaseHarvestTime",
                table: "NodeDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseHarvestTime",
                table: "NodeDefinitions");

            migrationBuilder.RenameColumn(
                name: "BaseQuantity",
                table: "NodeDefinitions",
                newName: "HarvestTime");
        }
    }
}
