using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations.Economy
{
    /// <inheritdoc />
    public partial class Econ_AddToolRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequiredTool",
                table: "NodeDefinitions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiredTool",
                table: "NodeDefinitions");
        }
    }
}
