using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Notes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture]
public class PlayerNoteTests
{
    private InMemoryPlayerCodexRepository _repository = null!;
    private InMemoryEventBus _events = null!;
    private CharacterId _characterId;
    private CommandDispatcher _commands = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new();
        _events = new();
        _characterId = CharacterId.New();
        _commands = new CommandDispatcher(new ICommandHandlerMarker[]
        {
            new AddNoteHandler(_repository, _events), new EditNoteHandler(_repository, _events),
            new DeleteNoteHandler(_repository, _events)
        }, _events);
    }

    private Task<CommandResult> Add(string content = "First line\nSecond line", string? title = "My note",
        NoteCategory category = NoteCategory.General) => _commands.DispatchAsync(new AddNoteCommand
    {
        CharacterId = _characterId, Content = content, Title = title, Category = category
    });

    [TestCase(NoteCategory.General)]
    [TestCase(NoteCategory.Quest)]
    [TestCase(NoteCategory.Character)]
    [TestCase(NoteCategory.Location)]
    public async Task CreateEditAndDelete_WithSystemCategory(NoteCategory category)
    {
        CommandResult added = await Add(category: category);
        Assert.That(added.Success, Is.True);
        Guid id = Guid.Parse(added.Data!["noteId"].ToString()!);
        PlayerCodex codex = (await _repository.LoadAsync(_characterId))!;
        Assert.That(codex.GetNote(id)!.Content, Is.EqualTo("First line\nSecond line"));

        CommandResult edited = await _commands.DispatchAsync(new EditNoteCommand
        {
            CharacterId = _characterId, NoteId = id, Title = "Updated", NewContent = "New body",
            Category = NoteCategory.Location
        });
        CodexNoteEntry note = codex.GetNote(id)!;
        Assert.Multiple(() =>
        {
            Assert.That(edited.Success, Is.True);
            Assert.That(note.Title, Is.EqualTo("Updated"));
            Assert.That(note.Category, Is.EqualTo(NoteCategory.Location));
            Assert.That(note.Content, Is.EqualTo("New body"));
            Assert.That(note.LastModified, Is.GreaterThanOrEqualTo(note.DateCreated));
            Assert.That(_events.PublishedEvents.OfType<NoteEditedEvent>().Single().Category,
                Is.EqualTo(NoteCategory.Location));
            Assert.That(_events.PublishedEvents.OfType<NoteAddedEvent>().Single().Title, Is.EqualTo("My note"));
        });
        Assert.That((await _commands.DispatchAsync(new DeleteNoteCommand
        {
            CharacterId = _characterId, NoteId = id
        })).Success, Is.True);
        Assert.That(codex.GetNote(id), Is.Null);
    }

    [TestCase(NoteCategory.DmNote)]
    [TestCase(NoteCategory.DmPrivate)]
    [TestCase((NoteCategory)999)]
    public async Task PlayerCannotCreateReservedOrInvalidCategory(NoteCategory category)
    {
        Assert.That((await Add(category: category)).Success, Is.False);
        Assert.That(await _repository.LoadAsync(_characterId), Is.Null);
        Assert.That(_events.PublishedEvents, Is.Empty);
    }

    [TestCase("")]
    [TestCase(" \n\t")]
    public async Task BlankBodyDoesNotCreateNote(string content)
    {
        Assert.That((await Add(content)).Success, Is.False);
        Assert.That(_repository.Count, Is.Zero);
    }

    [Test]
    public async Task InputLimitsRejectOverflowAndAcceptBoundary()
    {
        Assert.That((await Add(new string('x', CodexNoteEntry.MaxPlayerContentLength + 1))).Success, Is.False);
        Assert.That((await Add(title: new string('x', CodexNoteEntry.MaxTitleLength + 1))).Success, Is.False);
        Assert.That((await Add(new string('x', CodexNoteEntry.MaxPlayerContentLength),
            new string('x', CodexNoteEntry.MaxTitleLength))).Success, Is.True);
    }

    [Test]
    public async Task InvalidEditLeavesAllFieldsUnchanged()
    {
        await Add();
        PlayerCodex codex = (await _repository.LoadAsync(_characterId))!;
        CodexNoteEntry note = codex.Notes.Single();
        DateTime lastUpdated = codex.LastUpdated;
        CommandResult result = await _commands.DispatchAsync(new EditNoteCommand
        {
            CharacterId = _characterId, NoteId = note.Id, Title = "Changed", NewContent = " ",
            Category = NoteCategory.Location
        });
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(note.Title, Is.EqualTo("My note"));
            Assert.That(note.Content, Is.EqualTo("First line\nSecond line"));
            Assert.That(note.Category, Is.EqualTo(NoteCategory.General));
            Assert.That(codex.LastUpdated, Is.EqualTo(lastUpdated));
        });
    }

    [Test]
    public async Task OptionalTitleCanBeCleared()
    {
        await Add();
        PlayerCodex codex = (await _repository.LoadAsync(_characterId))!;
        CodexNoteEntry note = codex.Notes.Single();
        Assert.That((await _commands.DispatchAsync(new EditNoteCommand
        {
            CharacterId = _characterId, NoteId = note.Id, Title = " ", NewContent = "Body",
            Category = NoteCategory.General
        })).Success, Is.True);
        Assert.That(note.Title, Is.Null);
        Assert.That(new NoteDisplayItem(note).DisplayName, Is.EqualTo("Untitled Note"));
    }

    [TestCase(true, NoteCategory.General)]
    [TestCase(true, NoteCategory.DmNote)]
    [TestCase(true, NoteCategory.DmPrivate)]
    [TestCase(false, NoteCategory.DmNote)]
    [TestCase(false, NoteCategory.DmPrivate)]
    public async Task DmNotesCannotBeEditedOrDeleted(bool isDmNote, NoteCategory category)
    {
        PlayerCodex codex = new(_characterId, DateTime.UtcNow);
        CodexNoteEntry note = new(Guid.NewGuid(), "DM body", category, DateTime.UtcNow, isDmNote, false);
        codex.AddNote(note, DateTime.UtcNow);
        await _repository.SaveAsync(codex);
        Assert.That((await _commands.DispatchAsync(new EditNoteCommand
        {
            CharacterId = _characterId, NoteId = note.Id, Title = null, NewContent = "Changed",
            Category = NoteCategory.General
        })).Success, Is.False);
        Assert.That((await _commands.DispatchAsync(new DeleteNoteCommand
        {
            CharacterId = _characterId, NoteId = note.Id
        })).Success, Is.False);
        Assert.That(codex.GetNote(note.Id)!.Content, Is.EqualTo("DM body"));
    }

    [Test]
    public async Task AnotherCharactersNoteCannotBeMutated()
    {
        await Add();
        CodexNoteEntry note = (await _repository.LoadAsync(_characterId))!.Notes.Single();
        CharacterId other = CharacterId.New();
        Assert.That((await _commands.DispatchAsync(new EditNoteCommand
        {
            CharacterId = other, NoteId = note.Id, Title = null, NewContent = "Changed",
            Category = NoteCategory.General
        })).Success, Is.False);
        Assert.That((await _commands.DispatchAsync(new DeleteNoteCommand
        {
            CharacterId = other, NoteId = note.Id
        })).Success, Is.False);
        Assert.That(note.Content, Is.EqualTo("First line\nSecond line"));
    }

    [Test]
    public async Task PlayerQueryCombinesVisibilityCategoryAndSearchWithStableOrdering()
    {
        DateTime now = DateTime.UtcNow;
        PlayerCodex codex = new(_characterId, now);
        CodexNoteEntry personal = new(Guid.NewGuid(), "Dragon", NoteCategory.Quest, now, false, true);
        CodexNoteEntry visibleDm = new(Guid.NewGuid(), "Dragon", NoteCategory.DmNote, now, true, false);
        CodexNoteEntry titleMatch = new(Guid.NewGuid(), "Different body", NoteCategory.Quest, now, false, false, "DRAGON");
        titleMatch.UpdateTitle("DRAGON", now.AddMinutes(1));
        CodexNoteEntry[] notes =
        {
            personal, visibleDm, titleMatch,
            new(Guid.NewGuid(), "Dragon", NoteCategory.DmNote, now, true, true),
            new(Guid.NewGuid(), "Dragon", NoteCategory.DmPrivate, now, false, false),
            new(Guid.NewGuid(), "Dragon", NoteCategory.General, now, true, true),
            new(Guid.NewGuid(), "Unrelated", NoteCategory.General, now, false, false)
        };
        foreach (var note in notes) codex.AddNote(note, now);
        await _repository.SaveAsync(codex);
        GetPlayerCodexNotesHandler query = new(_repository);
        IReadOnlyList<CodexNoteEntry> all = await query.HandleAsync(new GetPlayerCodexNotesQuery
        {
            CharacterId = _characterId
        });
        Assert.That(all, Has.Count.EqualTo(4));
        Assert.That(all.First().Id, Is.EqualTo(titleMatch.Id));
        Assert.That(all.Where(n => n.LastModified == now).Select(n => n.Id), Is.Ordered);
        var filtered = await query.HandleAsync(new GetPlayerCodexNotesQuery
        {
            CharacterId = _characterId, Category = NoteCategory.Quest, SearchTerm = " dragon "
        });
        Assert.That(filtered.Select(n => n.Id), Is.EquivalentTo(new[] { personal.Id, titleMatch.Id }));
        Assert.That((await query.HandleAsync(new GetPlayerCodexNotesQuery
        {
            CharacterId = _characterId, Category = NoteCategory.DmPrivate, SearchTerm = "dragon"
        })), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PersistenceFailureReturnsFailureWithoutPublishingSuccess(bool failLoad)
    {
        FailingRepository repository = new(failLoad);
        InMemoryEventBus events = new();
        CommandDispatcher commands = new(new ICommandHandlerMarker[] { new AddNoteHandler(repository, events) }, events);
        CommandResult result = await commands.DispatchAsync(new AddNoteCommand
        {
            CharacterId = _characterId, Content = "Body", Category = NoteCategory.General
        });
        Assert.That(result.Success, Is.False);
        Assert.That(events.PublishedEvents, Is.Empty);
    }

    [Test]
    public void DraftTracksChangesWithoutMutatingSavedNote()
    {
        CodexNoteEntry note = new(Guid.NewGuid(), "Original", NoteCategory.General, DateTime.UtcNow, false, false, "Title");
        CodexNoteDraft draft = new(note, NoteCategory.Location);
        Assert.That(draft.IsDirty, Is.False);
        draft.Content = "Changed";
        Assert.That(draft.IsDirty, Is.True);
        Assert.That(note.Content, Is.EqualTo("Original"));
        draft.Content = "Original";
        draft.Category = NoteCategory.Quest;
        Assert.That(draft.IsDirty, Is.True);
        draft.Category = NoteCategory.General;
        draft.Title = "Changed title";
        Assert.That(draft.IsDirty, Is.True);
    }

    [Test]
    public void DraftSavedAfterForcedCloseRetainsItsIdAndAnyLaterChanges()
    {
        CodexNoteDraft draft = new(null, NoteCategory.General) { Title = "Title", Content = "Submitted" };
        Guid savedId = Guid.NewGuid();
        draft.Content = "Edited after reopening";
        draft.MarkSaved(savedId, "Title", "Submitted", NoteCategory.General);
        Assert.That(draft.NoteId, Is.EqualTo(savedId));
        Assert.That(draft.IsDirty, Is.True);
        Assert.That(draft.Content, Is.EqualTo("Edited after reopening"));
        draft.Content = "Submitted";
        Assert.That(draft.IsDirty, Is.False);
    }

    private sealed class FailingRepository(bool failLoad) : IPlayerCodexRepository
    {
        public Task<PlayerCodex?> LoadAsync(CharacterId characterId, CancellationToken ct = default) =>
            failLoad ? throw new InvalidOperationException("Load failed") : Task.FromResult<PlayerCodex?>(null);
        public Task SaveAsync(PlayerCodex codex, CancellationToken ct = default) => throw new InvalidOperationException("Save failed");
    }
}
