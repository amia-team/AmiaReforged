using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddRealmChaos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RealmChaos",
                table: "InvasionRecord",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RealmChaos",
                table: "InvasionRecord");
        }
    }
}
