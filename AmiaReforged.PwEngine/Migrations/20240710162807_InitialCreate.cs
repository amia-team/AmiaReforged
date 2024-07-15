using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AmiaReforged.PwEngine.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Characters",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Characters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StorageContainers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageContainers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ResRef = table.Column<string>(type: "text", nullable: false),
                    BaseValue = table.Column<int>(type: "integer", nullable: false),
                    MagicModifier = table.Column<float>(type: "real", nullable: false),
                    DurabilityModifier = table.Column<float>(type: "real", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quality = table.Column<int>(type: "integer", nullable: false),
                    Material = table.Column<int>(type: "integer", nullable: false),
                    WorldCharacterId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Characters_WorldCharacterId",
                        column: x => x.WorldCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemStorageUsers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemStorageId = table.Column<long>(type: "bigint", nullable: false),
                    WorldCharacterId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemStorageUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemStorageUsers_Characters_WorldCharacterId",
                        column: x => x.WorldCharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ItemStorageUsers_StorageContainers_ItemStorageId",
                        column: x => x.ItemStorageId,
                        principalTable: "StorageContainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoredJobItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JobItemId = table.Column<long>(type: "bigint", nullable: false),
                    ItemStorageId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoredJobItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoredJobItems_Items_JobItemId",
                        column: x => x.JobItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StoredJobItems_StorageContainers_ItemStorageId",
                        column: x => x.ItemStorageId,
                        principalTable: "StorageContainers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_WorldCharacterId",
                table: "Items",
                column: "WorldCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStorageUsers_ItemStorageId",
                table: "ItemStorageUsers",
                column: "ItemStorageId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemStorageUsers_WorldCharacterId",
                table: "ItemStorageUsers",
                column: "WorldCharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredJobItems_ItemStorageId",
                table: "StoredJobItems",
                column: "ItemStorageId");

            migrationBuilder.CreateIndex(
                name: "IX_StoredJobItems_JobItemId",
                table: "StoredJobItems",
                column: "JobItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemStorageUsers");

            migrationBuilder.DropTable(
                name: "StoredJobItems");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "StorageContainers");

            migrationBuilder.DropTable(
                name: "Characters");
        }
    }
}
