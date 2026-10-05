using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using AmiaReforged.PwEngine.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture, Category("CodexPostgres"), NonParallelizable]
public class CodexQuestAvailabilityPostgresTests
{
    [Test]
    public async Task DefaultStageMigrationPreservesExistingDefinitionsAndCharacterProgress()
    {
        await using PostgreSqlContainer postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await postgres.StartAsync();
        TestContextFactory factory = new(postgres.GetConnectionString());
        using PwEngineContext context = factory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        Guid characterId = Guid.NewGuid();
        context.Characters.Add(new PersistedCharacter
        {
            Id = characterId, FirstName = "Migration", LastName = "Test", CdKey = "QSTTEST2"
        });
        context.CodexQuestDefinitions.Add(new PersistedQuestDefinition
        {
            QuestId = "tutorial", Title = "Tutorial", Description = "An existing definition.", IsAlwaysAvailable = true,
            StagesJson = """[{"stageId":10,"journalText":"First step."}]"""
        });
        context.CodexQuests.Add(new PersistedCodexQuest
        {
            CharacterId = characterId, QuestId = "tutorial", Title = "Tutorial", Description = "An existing instance.",
            State = (int)QuestState.InProgress, CurrentStageId = 5, DateStarted = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        AddQuestDefaultStage migration = new();
        IMigrationsSqlGenerator generator = context.GetService<IMigrationsSqlGenerator>();
        foreach (MigrationCommand command in generator.Generate(migration.DownOperations, context.Model))
            await context.Database.ExecuteSqlRawAsync(command.CommandText);
        foreach (MigrationCommand command in generator.Generate(migration.UpOperations, context.Model))
            await context.Database.ExecuteSqlRawAsync(command.CommandText);

        PersistedQuestDefinition definition = await context.CodexQuestDefinitions.SingleAsync();
        Assert.That(definition.DefaultStageId, Is.Null);
        Assert.That((await context.CodexQuests.SingleAsync()).CurrentStageId, Is.EqualTo(5));
        definition.DefaultStageId = 10;
        await context.SaveChangesAsync();
        var codex = await new EfPlayerCodexRepository(factory).LoadAsync((CharacterId)characterId);
        Assert.That(codex!.Quests.Single().CurrentStageId, Is.EqualTo(5));
    }

    [TestCase(null, null)]
    [TestCase(10, null)]
    [TestCase(10, "Completed")]
    public async Task ConcurrentLoadsPersistMissingInstancesWithoutResettingExistingProgress(int? defaultStageId, string? stageState)
    {
        await using PostgreSqlContainer postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await postgres.StartAsync();
        TestContextFactory factory = new(postgres.GetConnectionString());
        CharacterId characterId = CharacterId.New();
        DateTime started = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        using (PwEngineContext context = factory.CreateDbContext())
        {
            await context.Database.EnsureCreatedAsync();
            context.Characters.Add(new PersistedCharacter
            {
                Id = characterId.Value, FirstName = "Quests", LastName = "Test", CdKey = "QSTTEST1"
            });
            foreach (string questId in new[] { "existing", "missing_one", "missing_two" })
            {
                context.CodexQuestDefinitions.Add(new PersistedQuestDefinition
                {
                    QuestId = questId, Title = "Lost Artifact", Description = "Find the artifact.",
                    IsAlwaysAvailable = true, CreatedUtc = started, QuestGiver = "Archivist", Location = "Ruins",
                    DefaultStageId = defaultStageId,
                    Keywords = "artifact,ruins", StagesJson = System.Text.Json.JsonSerializer.Serialize(new[]
                    {
                        new { StageId = 10, JournalText = "Search the ruins.", QuestState = stageState }
                    })
                });
            }
            context.CodexQuests.Add(new PersistedCodexQuest
            {
                CharacterId = characterId.Value, QuestId = "existing", Title = "Recorded quest",
                Description = "Recorded description", State = (int)QuestState.Completed, CurrentStageId = 10,
                DateStarted = started, DateCompleted = started.AddHours(1), CompletionCount = 2
            });
            await context.SaveChangesAsync();
        }

        DateTime beforeLoad = DateTime.UtcNow;
        var codices = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            new EfPlayerCodexRepository(factory).LoadAsync(characterId)));
        Assert.That(codices.All(c => c!.Quests.Count == 3), Is.True);

        using PwEngineContext persisted = factory.CreateDbContext();
        List<PersistedCodexQuest> rows = await persisted.CodexQuests.ToListAsync();
        Assert.That(rows, Has.Count.EqualTo(3));
        PersistedCodexQuest existing = rows.Single(q => q.QuestId == "existing");
        Assert.Multiple(() =>
        {
            Assert.That(existing.State, Is.EqualTo((int)QuestState.Completed));
            Assert.That(existing.CurrentStageId, Is.EqualTo(10));
            Assert.That(existing.DateStarted, Is.EqualTo(started));
            Assert.That(existing.DateCompleted, Is.EqualTo(started.AddHours(1)));
            Assert.That(existing.CompletionCount, Is.EqualTo(2));
        });
        foreach (PersistedCodexQuest created in rows.Where(q => q.QuestId != "existing"))
        {
            Assert.Multiple(() =>
            {
                Assert.That(created.CharacterId, Is.EqualTo(characterId.Value));
                Assert.That(created.State, Is.EqualTo((int)(stageState == "Completed" ? QuestState.Completed
                    : defaultStageId.HasValue ? QuestState.InProgress : QuestState.Discovered)));
                Assert.That(created.CurrentStageId, Is.EqualTo(defaultStageId ?? 0));
                Assert.That(created.DateCompleted, Is.EqualTo(stageState == "Completed" ? created.DateStarted : (DateTime?)null));
                Assert.That(created.DateStarted, Is.InRange(beforeLoad, DateTime.UtcNow));
                Assert.That(created.StagesJson, Does.Contain("Search the ruins."));
                Assert.That(created.QuestGiver, Is.EqualTo("Archivist"));
                Assert.That(created.Location, Is.EqualTo("Ruins"));
                Assert.That(created.Keywords, Is.EqualTo("artifact,ruins"));
            });
        }
    }

    private sealed class TestContextFactory(string connectionString) : IDbContextFactory<PwEngineContext>
    {
        public PwEngineContext CreateDbContext() => new(connectionString);
    }
}
