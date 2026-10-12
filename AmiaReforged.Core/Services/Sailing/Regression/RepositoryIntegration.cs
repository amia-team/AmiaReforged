#if SAILING_REGRESSION
using AmiaReforged.Core;
using AmiaReforged.Core.Models.Sailing;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Sailing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

static class RepositoryIntegration
{
    public static void Verify(Action<bool, string> check)
    {
        // Model inspection does not open a connection or execute database commands.
        using AmiaDbContext shared = new DatabaseContextFactory().CreateDbContext();
        IEntityType legacy = shared.Model.FindEntityType(typeof(SavedShipState))!;
        check(legacy.GetTableName() == "saved_ship_states", "Shared context preserves the existing ship table.");
        check(legacy.FindProperty(nameof(SavedShipState.ShipId)) == null &&
              legacy.FindProperty(nameof(SavedShipState.SnapshotJson)) == null,
              "Sailing additions must not change the shared context schema.");
        check(shared.Model.FindEntityType(typeof(SavedChartDiscovery)) == null,
              "Chart persistence does not expand the shared application model.");
        string[] expected = ["AreaResRef", "Heading", "Hull", "Id", "ShipName", "Underway", "WeaponResRef", "X", "Y", "Z"];
        check(legacy.GetProperties().Select(p => p.Name).Order().SequenceEqual(expected),
              "All ten legacy ship properties are preserved without new schema dependencies.");
        check(legacy.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "ShipName"),
              "The existing unique ship-name index is retained.");

        DbContextOptions<SailingPersistenceDbContext> options =
            new DbContextOptionsBuilder<SailingPersistenceDbContext>()
                .UseNpgsql("Host=localhost;Database=isolated_model_validation;Username=validation;Password=validation").Options;
        using SailingPersistenceDbContext sailing = SailingPersistenceContextFactory.ForOptions(options).CreateDbContext();
        IEntityType ships = sailing.Model.FindEntityType(typeof(SavedShipState))!;
        IEntityType charts = sailing.Model.FindEntityType(typeof(SavedChartDiscovery))!;
        check(ships.FindProperty("ShipId")!.GetColumnName() == "ship_id" &&
              ships.FindProperty("SnapshotJson")!.GetColumnName() == "snapshot_json",
              "Dedicated sailing context explicitly maps stable identity and snapshot payload.");
        foreach (IProperty property in legacy.GetProperties())
            check(ships.FindProperty(property.Name)!.GetColumnName() == property.GetColumnName(),
                  "Dedicated context preserves the legacy column mapping for " + property.Name + ".");
        check(ships.GetIndexes().Any(i => i.IsUnique && i.Properties.Single().Name == "ShipId"),
              "PostgreSQL model enforces unique non-null stable identities.");
        check(charts.GetTableName() == "saved_chart_discovery" && charts.FindProperty("Cells")!.GetMaxLength() == 256,
              "Chart model preserves the 16 by 16 discovery payload.");
        check(charts.GetIndexes().Any(i => i.IsUnique && i.Properties.Select(p => p.Name)
                .SequenceEqual(new[] { "AccountKey", "AreaResRef" })),
              "The sailing model uniquely identifies discovery by account and area.");
        string sql = sailing.Database.GenerateCreateScript();
        check(sql.Contains("ship_id") && sql.Contains("snapshot_json") && sql.Contains("saved_chart_discovery"),
              "Actual PostgreSQL provider generates the dedicated schema offline.");
        IModel sharedDesign = shared.GetService<IDesignTimeModel>().Model;
        IModel sailingDesign = sailing.GetService<IDesignTimeModel>().Model;
        IReadOnlyList<MigrationOperation> differences = sailing.GetService<IMigrationsModelDiffer>()
            .GetDifferences(sharedDesign.GetRelationalModel(), sailingDesign.GetRelationalModel());
        // The contexts have different scopes. Only the approved sailing additions
        // belong in the update script; unrelated shared-table differences are ignored.
        MigrationOperation[] additions = differences.Where(operation => operation switch
        {
            AddColumnOperation column => column.Table == "saved_ship_states" &&
                column.Name is "ship_id" or "snapshot_json",
            CreateTableOperation table => table.Name == "saved_chart_discovery",
            CreateIndexOperation index => index.Table == "saved_chart_discovery" ||
                (index.Table == "saved_ship_states" && index.Name == "IX_saved_ship_states_ship_id"),
            _ => false
        }).ToArray();
        check(additions.Length == 5 && additions.OfType<AddColumnOperation>().Count() == 2 &&
              additions.OfType<CreateTableOperation>().Count() == 1 &&
              additions.OfType<CreateIndexOperation>().Count() == 2,
              "Additive schema plan contains exactly two columns, one chart table and two indexes.");
        check(additions.OfType<AddColumnOperation>().All(c => c.IsNullable),
              "Legacy ship rows remain valid with null new columns.");
        string update = string.Join("\n", sailing.GetService<IMigrationsSqlGenerator>()
            .Generate(additions, sailingDesign).Select(command => command.CommandText));
        check(!update.Contains("DROP ", StringComparison.OrdinalIgnoreCase) &&
              !update.Contains("dreamcoin_records"), "Generated sailing update is additive and contains no unrelated tables.");
        string output = Path.Combine(Path.GetTempPath(), "amia-sailing-regression");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "sailing-additive-schema.sql"),
            "-- Generated with the actual PostgreSQL EF provider. Not executed.\n" +
            "-- Review and validate against an isolated database before selecting PostgreSQL sailing persistence.\n" + update);
        check(!sql.Contains("dreamcoin_records") && !sql.Contains("player_characters"),
              "Dedicated schema contains no unrelated application tables.");
        using SailingPersistenceDbContext configured = new SailingPersistenceContextFactory(new DatabaseContextFactory()).CreateDbContext();
        check(configured.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL",
              "Production sailing factory uses the real application's PostgreSQL provider.");
    }
}

#endif
