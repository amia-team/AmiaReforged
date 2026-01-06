using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class AddDreamcoinRentals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "dreamcoin_rentals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_cd_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    monthly_cost = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    created_by_dm_cd_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    is_delinquent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_payment_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_due_date_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("dreamcoin_rentals_pkey", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "dreamcoin_rentals_is_active_idx",
                table: "dreamcoin_rentals",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "dreamcoin_rentals_is_delinquent_idx",
                table: "dreamcoin_rentals",
                column: "is_delinquent");

            migrationBuilder.CreateIndex(
                name: "dreamcoin_rentals_next_due_date_idx",
                table: "dreamcoin_rentals",
                column: "next_due_date_utc");

            migrationBuilder.CreateIndex(
                name: "dreamcoin_rentals_player_cd_key_idx",
                table: "dreamcoin_rentals",
                column: "player_cd_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dreamcoin_rentals");
        }
    }
}
