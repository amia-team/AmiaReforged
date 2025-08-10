using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations.Economy
{
    /// <inheritdoc />
    public partial class Economy_DropRichnessFromModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Richness",
                table: "NodeInstances");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "Richness",
                table: "NodeInstances",
                type: "real",
                nullable: false,
                defaultValue: 0f);
        }
    }
}
