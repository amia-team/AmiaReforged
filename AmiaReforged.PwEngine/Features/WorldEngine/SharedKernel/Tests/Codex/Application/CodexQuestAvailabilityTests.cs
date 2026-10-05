using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture]
public class CodexQuestAvailabilityTests
{
    private TestContextFactory _factory = null!;
    private EfPlayerCodexRepository _repository = null!;
    private CharacterId _characterId;
    private readonly DateTime _created = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PwEngineContext> options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _factory = new TestContextFactory(options);
        _repository = new EfPlayerCodexRepository(_factory);
        _characterId = CharacterId.New();
    }

    [Test]
    public async Task AlwaysAvailableQuestIsPersistedForCharactersWithoutAnyJournalRecords()
    {
        await AddDefinitionAsync(true);
        await AddDefinitionAsync(false, "hidden_quest");
        DateTime beforeLoad = DateTime.UtcNow;

        CharacterId[] characters = { _characterId, CharacterId.New() };
        foreach (CharacterId character in characters)
        {
            IReadOnlyList<CodexQuestEntry> quests = await new GetCodexQuestsHandler(_repository)
                .HandleAsync(new GetCodexQuestsQuery { CharacterId = character });
            Assert.That(quests, Has.Count.EqualTo(1));
            CodexQuestEntry quest = quests.Single();
            Assert.Multiple(() =>
            {
                Assert.That(quest.QuestId.Value, Is.EqualTo("public_quest"));
                Assert.That(quest.Title, Is.EqualTo("Lost Artifact"));
                Assert.That(quest.Description, Is.EqualTo("Find the artifact."));
                Assert.That(quest.State, Is.EqualTo(QuestState.Discovered));
                Assert.That(quest.CurrentStageId, Is.Zero);
                Assert.That(quest.DateCompleted, Is.Null);
                Assert.That(quest.DateStarted, Is.InRange(beforeLoad, DateTime.UtcNow));
                Assert.That(quest.QuestGiver, Is.EqualTo("Archivist"));
                Assert.That(quest.Location, Is.EqualTo("Ruins"));
                Assert.That(quest.Keywords.Select(k => k.Value), Is.EqualTo(new[] { "artifact", "ruins" }));
                Assert.That(quest.Stages.Single().JournalText, Is.EqualTo("Search the ruins."));
            });

            using PwEngineContext persisted = _factory.CreateDbContext();
            PersistedCodexQuest row = await persisted.CodexQuests.SingleAsync(q => q.CharacterId == character.Value);
            Assert.Multiple(() =>
            {
                Assert.That(row.QuestId, Is.EqualTo(quest.QuestId.Value));
                Assert.That(row.State, Is.EqualTo((int)QuestState.Discovered));
                Assert.That(row.CurrentStageId, Is.Zero);
                Assert.That(row.DateStarted, Is.EqualTo(quest.DateStarted));
                Assert.That(row.StagesJson, Does.Contain("Search the ruins."));
            });
        }

        using PwEngineContext context = _factory.CreateDbContext();
        Assert.That(await context.CodexQuests.CountAsync(), Is.EqualTo(characters.Length));
        Assert.That(await context.CodexQuests.AnyAsync(q => q.QuestId == "hidden_quest"), Is.False);
    }

    [TestCase(QuestState.Discovered)]
    [TestCase(QuestState.InProgress)]
    [TestCase(QuestState.Completed)]
    [TestCase(QuestState.Failed)]
    [TestCase(QuestState.Abandoned)]
    [TestCase(QuestState.Expired)]
    public async Task AlwaysAvailableDefinitionDoesNotDuplicateOrResetRecordedProgress(QuestState state)
    {
        await AddDefinitionAsync(true, defaultStageId: 10);
        DateTime started = _created.AddDays(1);
        DateTime? completed = state is QuestState.Discovered or QuestState.InProgress ? null : started.AddHours(1);
        using (PwEngineContext context = _factory.CreateDbContext())
        {
            context.CodexQuests.Add(new PersistedCodexQuest
            {
                CharacterId = _characterId.Value, QuestId = "public_quest", Title = "Recorded quest",
                Description = "Recorded description", State = (int)state, CurrentStageId = 10,
                DateStarted = started, DateCompleted = completed, CompletionCount = 2
            });
            await context.SaveChangesAsync();
        }

        PlayerCodex codex = (await _repository.LoadAsync(_characterId))!;
        Assert.That(codex.Quests, Has.Count.EqualTo(1));
        CodexQuestEntry quest = codex.Quests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(quest.State, Is.EqualTo(state));
            Assert.That(quest.CurrentStageId, Is.EqualTo(10));
            Assert.That(quest.DateStarted, Is.EqualTo(started));
            Assert.That(quest.DateCompleted, Is.EqualTo(completed));
            Assert.That(quest.CompletionCount, Is.EqualTo(2));
            Assert.That(quest.Stages, Has.Count.EqualTo(1));
        });
    }

    [TestCase(null, false, QuestState.InProgress)]
    [TestCase(null, true, QuestState.Completed)]
    [TestCase("Discovered", false, QuestState.Discovered)]
    [TestCase("InProgress", false, QuestState.InProgress)]
    [TestCase("Completed", false, QuestState.Completed)]
    [TestCase("Failed", false, QuestState.Failed)]
    [TestCase("Abandoned", false, QuestState.Abandoned)]
    [TestCase("Expired", false, QuestState.Expired)]
    public async Task AlwaysAvailableQuestStartsAtDefaultStageWithItsState(
        string? stageState, bool legacyCompletion, QuestState expectedState)
    {
        await AddDefinitionAsync(true, defaultStageId: 10);
        using (PwEngineContext context = _factory.CreateDbContext())
        {
            (await context.CodexQuestDefinitions.SingleAsync()).StagesJson = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new { StageId = 10, JournalText = "Start the tutorial.", QuestState = stageState, IsCompletionStage = legacyCompletion }
            });
            await context.SaveChangesAsync();
        }

        CodexQuestEntry quest = (await _repository.LoadAsync(_characterId))!.Quests.Single();
        using PwEngineContext persisted = _factory.CreateDbContext();
        PersistedCodexQuest row = await persisted.CodexQuests.SingleAsync();
        bool terminal = expectedState is QuestState.Completed or QuestState.Failed or QuestState.Abandoned or QuestState.Expired;
        Assert.Multiple(() =>
        {
            Assert.That(quest.CurrentStageId, Is.EqualTo(10));
            Assert.That(quest.State, Is.EqualTo(expectedState));
            Assert.That(quest.EffectiveState, Is.EqualTo(expectedState));
            Assert.That(row.CurrentStageId, Is.EqualTo(10));
            Assert.That(row.State, Is.EqualTo((int)expectedState));
            Assert.That(row.DateCompleted, Is.EqualTo(terminal ? row.DateStarted : (DateTime?)null));
        });
    }

    [Test]
    public async Task ExplicitCreationCanStartBelowDefaultWithoutInstantiatingTheDefaultFirst()
    {
        await AddDefinitionAsync(true, defaultStageId: 10);
        await AddDefinitionAsync(true, "another_quest");
        QuestId questId = (QuestId)"public_quest";
        PlayerCodex codex = (await _repository.LoadAsync(_characterId, questId, CancellationToken.None))!;
        Assert.That(codex.HasQuest(questId), Is.False);
        Assert.That(codex.HasQuest((QuestId)"another_quest"), Is.True);

        CodexQuestEntry explicitQuest = new()
        {
            QuestId = questId, Title = "Explicit quest", Description = "Starts at stage 5.", DateStarted = DateTime.UtcNow
        };
        codex.RecordQuestStarted(explicitQuest, explicitQuest.DateStarted);
        codex.AdvanceQuestStage(questId, 5, explicitQuest.DateStarted);
        await _repository.SaveAsync(codex);

        CodexQuestEntry reloaded = (await _repository.LoadAsync(_characterId))!.GetQuest(questId)!;
        Assert.That(reloaded.CurrentStageId, Is.EqualTo(5));
    }

    [Test]
    public async Task AvailabilityChangesAreVisibleOnNextQuery()
    {
        await AddDefinitionAsync(false);
        Assert.That(await _repository.LoadAsync(_characterId), Is.Null);

        using (PwEngineContext context = _factory.CreateDbContext())
        {
            (await context.CodexQuestDefinitions.SingleAsync()).IsAlwaysAvailable = true;
            await context.SaveChangesAsync();
        }

        IReadOnlyList<CodexQuestEntry> quests = await new GetCodexQuestsByStateHandler(_repository)
            .HandleAsync(new GetCodexQuestsByStateQuery { CharacterId = _characterId, State = QuestState.Discovered });
        Assert.That(quests, Has.Count.EqualTo(1));
        IReadOnlyList<CodexQuestEntry> matches = await new SearchCodexQuestsHandler(_repository)
            .HandleAsync(new SearchCodexQuestsQuery { CharacterId = _characterId, SearchTerm = "artifact" });
        Assert.That(matches, Has.Count.EqualTo(1));

        using PwEngineContext persisted = _factory.CreateDbContext();
        Assert.That(await persisted.CodexQuests.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task InstantiatedQuestSurvivesAvailabilityBeingDisabledAndRetainsProgress()
    {
        await AddDefinitionAsync(true);
        PlayerCodex codex = (await _repository.LoadAsync(_characterId))!;
        CodexQuestEntry original = codex.Quests.Single();
        codex.AdvanceQuestStage(original.QuestId, 10, DateTime.UtcNow);
        await _repository.SaveAsync(codex);

        using (PwEngineContext context = _factory.CreateDbContext())
        {
            (await context.CodexQuestDefinitions.SingleAsync()).IsAlwaysAvailable = false;
            await context.SaveChangesAsync();
        }

        for (int i = 0; i < 2; i++)
        {
            CodexQuestEntry reloaded = (await _repository.LoadAsync(_characterId))!.Quests.Single();
            Assert.Multiple(() =>
            {
                Assert.That(reloaded.State, Is.EqualTo(QuestState.InProgress));
                Assert.That(reloaded.CurrentStageId, Is.EqualTo(10));
                Assert.That(reloaded.DateStarted, Is.EqualTo(original.DateStarted));
            });
        }

        Assert.That(await _repository.LoadAsync(CharacterId.New()), Is.Null);
        using PwEngineContext persisted = _factory.CreateDbContext();
        Assert.That(await persisted.CodexQuests.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task AlwaysAvailableQuestCanBeDiscoveredAndStartedByEvents()
    {
        await AddDefinitionAsync(true);
        SaveObserver observer = new(_repository);
        CodexEventProcessor processor = new(observer);
        try
        {
            await processor.EnqueueEventAsync(new QuestDiscoveredEvent(_characterId, _created,
                (QuestId)"public_quest", "Lost Artifact", "Find the artifact."));
            await observer.Saved.Task.WaitAsync(TimeSpan.FromSeconds(5));
            observer.Saved = new(TaskCreationOptions.RunContinuationsAsynchronously);
            await processor.EnqueueEventAsync(new QuestStartedEvent(_characterId, _created.AddDays(1),
                (QuestId)"public_quest", "Lost Artifact", "Find the artifact."));
            await observer.Saved.Task.WaitAsync(TimeSpan.FromSeconds(5));

            CodexQuestEntry quest = (await _repository.LoadAsync(_characterId))!.Quests.Single();
            Assert.Multiple(() =>
            {
                Assert.That(quest.State, Is.EqualTo(QuestState.InProgress));
                Assert.That(quest.Stages, Has.Count.EqualTo(1));
                Assert.That(quest.CurrentStageId, Is.Zero);
            });
        }
        finally { await processor.StopAsync(); }
    }

    private async Task AddDefinitionAsync(bool available, string questId = "public_quest", int? defaultStageId = null)
    {
        using PwEngineContext context = _factory.CreateDbContext();
        context.CodexQuestDefinitions.Add(new PersistedQuestDefinition
        {
            QuestId = questId, Title = "Lost Artifact", Description = "Find the artifact.",
            IsAlwaysAvailable = available, CreatedUtc = _created, QuestGiver = "Archivist", Location = "Ruins",
            DefaultStageId = defaultStageId,
            Keywords = "artifact, ruins", StagesJson = """
                [{"stageId":10,"journalText":"Search the ruins."}]
                """
        });
        await context.SaveChangesAsync();
    }

    private sealed class TestContextFactory(DbContextOptions<PwEngineContext> options) : IDbContextFactory<PwEngineContext>
    {
        public PwEngineContext CreateDbContext() => new(options);
    }

    private sealed class SaveObserver(IPlayerCodexRepository inner) : IPlayerCodexRepository
    {
        public TaskCompletionSource Saved { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<PlayerCodex?> LoadAsync(CharacterId id, CancellationToken ct = default) => inner.LoadAsync(id, ct);
        public async Task SaveAsync(PlayerCodex codex, CancellationToken ct = default)
        {
            await inner.SaveAsync(codex, ct);
            Saved.TrySetResult();
        }
    }
}
