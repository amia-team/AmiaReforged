using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Notes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Testcontainers.PostgreSql;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture, Category("CodexPostgres"), NonParallelizable]
public class CodexNotesPersistenceTests
{
    private PostgreSqlContainer _postgres = null!;
    private TestContextFactory _factory = null!;
    private EfPlayerCodexRepository _repository = null!;
    private CharacterId _characterId;

    [OneTimeSetUp]
    public async Task StartDatabase()
    {
        _postgres = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
        await _postgres.StartAsync();
        _factory = new TestContextFactory(_postgres.GetConnectionString());
        using var context = _factory.CreateDbContext();
        await context.Database.EnsureCreatedAsync();
        _repository = new EfPlayerCodexRepository(_factory);
    }

    [OneTimeTearDown]
    public async Task StopDatabase()
    {
        if (_postgres != null) await _postgres.DisposeAsync();
    }

    [SetUp]
    public async Task CreateCharacter()
    {
        _characterId = CharacterId.New();
        using var context = _factory.CreateDbContext();
        context.Characters.Add(new PersistedCharacter
        {
            Id = _characterId.Value, FirstName = "Notes", LastName = "Test", CdKey = "NOTETEST"
        });
        await context.SaveChangesAsync();
    }

    [Test]
    public async Task NotesRoundTripTitleBodyCategoryAndBothTimestamps()
    {
        DateTime created = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime modified = created.AddDays(2);
        PlayerCodex codex = new(_characterId, created);
        CodexNoteEntry note = new(Guid.NewGuid(), "Original", NoteCategory.General, created, false, false, "Old title");
        codex.AddNote(note, created);
        await _repository.SaveAsync(codex);
        codex.EditNote(note.Id, "Updated title", "First line\nSecond line — 記録", NoteCategory.Location, modified);
        await _repository.SaveAsync(codex);

        CodexNoteEntry reloaded = (await _repository.LoadAsync(_characterId))!.GetNote(note.Id)!;
        Assert.Multiple(() =>
        {
            Assert.That(reloaded.Title, Is.EqualTo("Updated title"));
            Assert.That(reloaded.Content, Is.EqualTo("First line\nSecond line — 記録"));
            Assert.That(reloaded.Category, Is.EqualTo(NoteCategory.Location));
            Assert.That(reloaded.DateCreated, Is.EqualTo(created));
            Assert.That(reloaded.LastModified, Is.EqualTo(modified));
        });
        PlayerCodex loaded = (await _repository.LoadAsync(_characterId))!;
        loaded.DeleteNote(note.Id, modified.AddDays(1));
        await _repository.SaveAsync(loaded);
        Assert.That(await _repository.LoadAsync(_characterId), Is.Null);
    }

    [Test]
    public async Task FailedSaveRollsBackNotesAlreadyDeletedByReconciliation()
    {
        DateTime now = DateTime.UtcNow;
        PlayerCodex codex = new(_characterId, now);
        CodexNoteEntry original = new(Guid.NewGuid(), "Keep this", NoteCategory.General, now, false, false);
        codex.AddNote(original, now);
        await _repository.SaveAsync(codex);
        codex.DeleteNote(original.Id, now);
        codex.AddNote(new CodexNoteEntry(Guid.NewGuid(), "Invalid title", NoteCategory.General, now, false, false,
            new string('x', 201)), now);

        Assert.ThrowsAsync<DbUpdateException>(() => _repository.SaveAsync(codex));
        PlayerCodex? reloaded = await _repository.LoadAsync(_characterId);
        Assert.That(reloaded!.Notes.Select(n => n.Id), Is.EqualTo(new[] { original.Id }));
    }

    [Test]
    public async Task ForeignKeyFailureIsReportedAndPublishesNoNoteEvent()
    {
        InMemoryEventBus events = new();
        CommandDispatcher commands = new(new ICommandHandlerMarker[] { new AddNoteHandler(_repository, events) }, events);
        CommandResult result = await commands.DispatchAsync(new AddNoteCommand
        {
            CharacterId = CharacterId.New(), Content = "Body", Category = NoteCategory.General
        });
        Assert.That(result.Success, Is.False);
        Assert.That(events.PublishedEvents, Is.Empty);
    }

    [Test]
    public void LoadFailureIsNotReportedAsAnEmptyCodex()
    {
        EfPlayerCodexRepository repository = new(new ThrowingContextFactory());
        Assert.ThrowsAsync<InvalidOperationException>(() => repository.LoadAsync(_characterId));
    }

    [Test]
    public async Task ConcurrentNoteCommandsAndQuestEventPreserveSeparateEntries()
    {
        QuestSaveObserver observer = new(_repository);
        CodexEventProcessor processor = new(observer);
        InMemoryEventBus events = new();
        AddNoteHandler handler = new(observer, events);
        try
        {
            Task<CommandResult>[] adds = Enumerable.Range(0, 12).Select(i => handler.HandleAsync(new AddNoteCommand
            {
                CharacterId = _characterId, Content = $"Personal note {i}", Category = NoteCategory.General
            })).ToArray();
            await processor.EnqueueEventAsync(new QuestStartedEvent(_characterId, DateTime.UtcNow,
                (QuestId)"notes_concurrency_quest", "Separate quest", "Quest description"));
            CommandResult[] results = await Task.WhenAll(adds);
            await observer.QuestSaved.Task.WaitAsync(TimeSpan.FromSeconds(10));
            PlayerCodex codex = (await _repository.LoadAsync(_characterId))!;
            Assert.Multiple(() =>
            {
                Assert.That(results.All(r => r.Success), Is.True);
                Assert.That(codex.Notes, Has.Count.EqualTo(12));
                Assert.That(codex.Quests, Has.Count.EqualTo(1));
                Assert.That(codex.Notes.Select(n => n.Content), Is.EquivalentTo(
                    Enumerable.Range(0, 12).Select(i => $"Personal note {i}")));
            });
        }
        finally { await processor.StopAsync(); }
    }

    private sealed class QuestSaveObserver(IPlayerCodexRepository inner) : IPlayerCodexRepository
    {
        public TaskCompletionSource QuestSaved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<PlayerCodex?> LoadAsync(CharacterId id, CancellationToken ct = default) => inner.LoadAsync(id, ct);
        public async Task SaveAsync(PlayerCodex codex, CancellationToken ct = default)
        {
            await inner.SaveAsync(codex, ct);
            if (codex.Quests.Count > 0) QuestSaved.TrySetResult();
        }
    }

    private sealed class TestContextFactory(string connectionString) : IDbContextFactory<PwEngineContext>
    {
        public PwEngineContext CreateDbContext() => new(connectionString);
    }

    private sealed class ThrowingContextFactory : IDbContextFactory<PwEngineContext>
    {
        public PwEngineContext CreateDbContext() => throw new InvalidOperationException("Database unavailable");
    }
}
