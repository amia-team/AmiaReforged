using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Tests;

[TestFixture]
public class DialogueMigrationTests
{
    [Test]
    public void OwnershipMigrationGeneratesGuardedPostgresSqlAndRetainsItsTargetModel()
    {
        // SQL generation requires no database connection or native game server.
        using PwEngineContext context = new(new DbContextOptionsBuilder<PwEngineContext>()
            .UseNpgsql("Host=localhost;Database=dialogue_migration_test;Username=test;Password=test").Options);
        const string id = "20261002150000_UniqueDialogueSpeakerTag";
        string sql = context.GetService<IMigrator>().GenerateScript("20261001151124_GlyphModules", id);
        Assert.That(sql, Does.Contain("Duplicate dialogue speaker tags must be reassigned"));
        Assert.That(sql, Does.Contain("CREATE UNIQUE INDEX").And.Contain("speaker_tag IS NOT NULL AND speaker_tag <> ''"));
        Assert.That(sql.IndexOf("RAISE EXCEPTION", StringComparison.Ordinal), Is.LessThan(sql.IndexOf("DROP INDEX", StringComparison.Ordinal)));

        IMigrationsAssembly assembly = context.GetService<IMigrationsAssembly>();
        Migration migration = assembly.CreateMigration(assembly.Migrations[id], context.Database.ProviderName!);
        var index = migration.TargetModel.FindEntityType(typeof(PersistedDialogueTree).FullName!)!.GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(PersistedDialogueTree.SpeakerTag));
        Assert.That(index.IsUnique, Is.True);
        Assert.That(index.GetFilter(), Is.EqualTo("speaker_tag IS NOT NULL AND speaker_tag <> ''"));
    }
}
