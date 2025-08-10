using System.Collections.Generic;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations.Economy
{
    /// <inheritdoc />
    public partial class Econ_Add_Item_Yield_List : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<ResourceNodeDefinition.YieldItem>>(
                name: "YieldItems",
                table: "NodeDefinitions",
                type: "jsonb",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YieldItems",
                table: "NodeDefinitions");
        }
    }
}
