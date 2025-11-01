using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class ConfigureRentals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rentable_properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    internal_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    settlement_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    monthly_rent = table.Column<int>(type: "integer", nullable: false),
                    allows_coinhouse_rental = table.Column<bool>(type: "boolean", nullable: false),
                    allows_direct_rental = table.Column<bool>(type: "boolean", nullable: false),
                    settlement_coinhouse_tag = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    purchase_price = table.Column<int>(type: "integer", nullable: true),
                    monthly_ownership_tax = table.Column<int>(type: "integer", nullable: true),
                    eviction_grace_days = table.Column<int>(type: "integer", nullable: false),
                    default_owner_persona = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    occupancy_status = table.Column<int>(type: "integer", nullable: false),
                    current_tenant_persona = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    current_owner_persona = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    rental_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    next_payment_due_date = table.Column<DateOnly>(type: "date", nullable: true),
                    rental_monthly_rent = table.Column<int>(type: "integer", nullable: true),
                    rental_payment_method = table.Column<int>(type: "integer", nullable: true),
                    last_occupant_seen_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentable_properties", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "rentable_properties_internal_name_idx",
                table: "rentable_properties",
                column: "internal_name");

            migrationBuilder.CreateIndex(
                name: "rentable_properties_settlement_idx",
                table: "rentable_properties",
                column: "settlement_tag");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rentable_properties");
        }
    }
}
