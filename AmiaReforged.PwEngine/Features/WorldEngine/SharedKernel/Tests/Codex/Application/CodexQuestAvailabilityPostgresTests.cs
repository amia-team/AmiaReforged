using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture, Category("CodexPostgres"), NonParallelizable]
public class CodexQuestAvailabilityPostgresTests
{
    [Test]
    public async Task ConcurrentLoadsPersistMissingInstancesWithoutResettingExistingProgress()
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
                    Keywords = "artifact,ruins", StagesJson = """
                        [{"stageId":10,"journalText":"Search the ruins."}]
                        """
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
                Assert.That(created.State, Is.EqualTo((int)QuestState.Discovered));
                Assert.That(created.CurrentStageId, Is.Zero);
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
