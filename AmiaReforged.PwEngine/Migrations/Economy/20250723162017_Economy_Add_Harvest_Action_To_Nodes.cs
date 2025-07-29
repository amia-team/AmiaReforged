#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace AmiaReforged.PwEngine.Migrations.Economy
{
    /// <inheritdoc />
    public partial class Economy_Add_Harvest_Action_To_Nodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HarvestAction",
                table: "NodeDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HarvestAction",
                table: "NodeDefinitions");
        }
    }
}
