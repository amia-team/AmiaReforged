using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.DynamicQuests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Notes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Reputation;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

/// <summary>
/// Behavioral specs for the player-state codex commands introduced by the F-6 fix
/// (notes, reputation, traits, dynamic quests). Each spec dispatches through the
/// real <see cref="CommandDispatcher"/>/<see cref="QueryDispatcher"/> against
/// in-memory repositories.
/// </summary>
[TestFixture]
public class CodexPlayerStateBehavior
{
    private Mock<IEventBus> _eventBusMock = null!;
    private InMemoryPlayerCodexRepository _codexRepo = null!;
    private CharacterId _characterId;

    [SetUp]
    public void SetUp()
    {
        _eventBusMock = new Mock<IEventBus>();
        _eventBusMock
            .Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _codexRepo = new InMemoryPlayerCodexRepository();
        _characterId = CharacterId.New();
    }

    [TearDown]
    public void TearDown()
    {
        _codexRepo.Clear();
    }

    private CommandDispatcher Commands(params ICommandHandlerMarker[] handlers) =>
        new(handlers, _eventBusMock.Object);

    private QueryDispatcher Queries(params IQueryHandlerMarker[] handlers) =>
        new(handlers);

    // === Notes ===

    [Test]
    public async Task Note_AddAndRead_RoundTripsThroughDispatch()
    {
        CommandDispatcher commands = Commands(new AddNoteHandler(_codexRepo, _eventBusMock.Object));
        QueryDispatcher queries = Queries(
            new GetCodexNotesHandler(_codexRepo),
            new SearchCodexNotesHandler(_codexRepo));

        CommandResult added = await commands.DispatchAsync(new AddNoteCommand
        {
            CharacterId = _characterId,
            Content = "The vault is behind the waterfall",
            Category = NoteCategory.General
        });
        Assert.That(added.Success, Is.True);

        IReadOnlyList<CodexNoteEntry> notes = await queries.DispatchAsync<GetCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>(
            new GetCodexNotesQuery { CharacterId = _characterId });
        Assert.That(notes, Has.Count.EqualTo(1));
        Assert.That(notes[0].Content, Is.EqualTo("The vault is behind the waterfall"));

        _eventBusMock.Verify(b => b.PublishAsync(
            It.Is<NoteAddedEvent>(e => e.CharacterId == _characterId),
            It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(b => b.PublishAsync(
            It.Is<CommandExecutedEvent<AddNoteCommand>>(e => e.Result.Success),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Note_EditMissing_ReturnsFail()
    {
        CommandDispatcher commands = Commands(new EditNoteHandler(_codexRepo, _eventBusMock.Object));

        CommandResult result = await commands.DispatchAsync(new EditNoteCommand
        {
            CharacterId = _characterId,
            NoteId = Guid.NewGuid(),
            NewContent = "edited"
        });

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task Note_EditAndDelete_BehaveLikeCodex()
    {
        CommandDispatcher commands = Commands(
            new AddNoteHandler(_codexRepo, _eventBusMock.Object),
            new EditNoteHandler(_codexRepo, _eventBusMock.Object),
            new DeleteNoteHandler(_codexRepo, _eventBusMock.Object));

        CommandResult added = await commands.DispatchAsync(new AddNoteCommand
        {
            CharacterId = _characterId,
            Content = "original",
            Category = NoteCategory.Quest
        });
        Assert.That(added.Success, Is.True);

        PlayerCodex? codex = await _codexRepo.LoadAsync(_characterId);
        Assert.That(codex, Is.Not.Null);
        Guid noteId = codex!.Notes.Single().Id;

        CommandResult edited = await commands.DispatchAsync(new EditNoteCommand
        {
            CharacterId = _characterId,
            NoteId = noteId,
            NewContent = "updated"
        });
        Assert.That(edited.Success, Is.True);

        CommandResult deleted = await commands.DispatchAsync(new DeleteNoteCommand
        {
            CharacterId = _characterId,
            NoteId = noteId
        });
        Assert.That(deleted.Success, Is.True);

        CommandResult deletedAgain = await commands.DispatchAsync(new DeleteNoteCommand
        {
            CharacterId = _characterId,
            NoteId = noteId
        });
        Assert.That(deletedAgain.Success, Is.False);
    }

    // === Reputation ===

    [Test]
    public async Task Reputation_Adjust_PublishesEventAndReadsBack()
    {
        CommandDispatcher commands = Commands(new AdjustReputationHandler(_codexRepo, _eventBusMock.Object));
        QueryDispatcher queries = Queries(
            new GetCodexReputationHandler(_codexRepo),
            new GetCodexPositiveReputationsHandler(_codexRepo));

        CommandResult result = await commands.DispatchAsync(new AdjustReputationCommand
        {
            CharacterId = _characterId,
            FactionId = "harpers",
            FactionName = "Harpers",
            Delta = 5,
            Reason = "Returned the stolen ledger"
        });
        Assert.That(result.Success, Is.True);

        FactionReputation? reputation = await queries.DispatchAsync<GetCodexReputationQuery, FactionReputation?>(
            new GetCodexReputationQuery
            {
                CharacterId = _characterId,
                FactionId = new FactionId("harpers")
            });
        Assert.That(reputation, Is.Not.Null);

        _eventBusMock.Verify(b => b.PublishAsync(
            It.Is<ReputationChangedEvent>(e => e.Delta == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // === Traits ===

    [Test]
    public async Task Trait_RecordAndDuplicate_BehavesLikeCodex()
    {
        CommandDispatcher commands = Commands(new RecordTraitAcquiredHandler(_codexRepo, _eventBusMock.Object));

        RecordTraitAcquiredCommand command = new()
        {
            CharacterId = _characterId,
            TraitTag = "brave",
            Name = "Brave",
            Description = "Stands firm when others flee.",
            Category = TraitCategory.Personality,
            AcquisitionMethod = "Character Creation"
        };

        CommandResult first = await commands.DispatchAsync(command);
        Assert.That(first.Success, Is.True);

        CommandResult duplicate = await commands.DispatchAsync(command);
        Assert.That(duplicate.Success, Is.False);

        _eventBusMock.Verify(b => b.PublishAsync(
            It.Is<TraitAcquiredEvent>(e => e.TraitTag == new TraitTag("brave")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // === Dynamic quests (service behind commands) ===

    private DynamicQuestService QuestService(InMemoryDynamicQuestRepository dynRepo, IEventBus? eventBus = null)
    {
        IEventBus bus = eventBus ?? new InMemoryEventBus();
        ObjectiveEvaluatorRegistry registry = new([]);
        QuestSessionManager sessions = new(registry);
        CodexEventProcessor processor = new(_codexRepo);
        return new DynamicQuestService(dynRepo, sessions, processor, bus);
    }

    [Test]
    public async Task DynamicQuest_Post_PublishesDomainEventOnBus()
    {
        InMemoryEventBus bus = new();
        QuestPostedEvent? observed = null;
        bus.Subscribe<QuestPostedEvent>((@event, _) =>
        {
            observed = @event;
            return Task.CompletedTask;
        });

        InMemoryDynamicQuestRepository dynRepo = new();
        DynamicQuestTemplate template = new()
        {
            TemplateId = TemplateId.NewId(),
            Title = "Goblin Scouts",
            Description = "Clear the goblin camp.",
        };
        await dynRepo.SaveTemplateAsync(template);

        CommandDispatcher commands = Commands(new PostDynamicQuestHandler(QuestService(dynRepo, bus)));

        CommandResult result = await commands.DispatchAsync(new PostDynamicQuestCommand
        {
            TemplateId = template.TemplateId.Value,
            PostedBy = _characterId
        });

        Assert.That(result.Success, Is.True);
        Assert.That(observed, Is.Not.Null,
            "A bus subscriber must observe QuestPostedEvent on a successful post");
        Assert.That(observed!.Title, Is.EqualTo("Goblin Scouts"));
    }

    [Test]
    public async Task DynamicQuest_PostMissingTemplate_ReturnsFail()
    {
        InMemoryDynamicQuestRepository dynRepo = new();
        CommandDispatcher commands = Commands(new PostDynamicQuestHandler(QuestService(dynRepo)));

        CommandResult result = await commands.DispatchAsync(new PostDynamicQuestCommand
        {
            TemplateId = Guid.NewGuid(),
            PostedBy = _characterId
        });

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task DynamicQuest_ClaimMissingPosting_ReturnsFail()
    {
        InMemoryDynamicQuestRepository dynRepo = new();
        CommandDispatcher commands = Commands(new ClaimDynamicQuestHandler(QuestService(dynRepo)));

        CommandResult result = await commands.DispatchAsync(new ClaimDynamicQuestCommand
        {
            CharacterId = _characterId,
            PostingId = Guid.NewGuid()
        });

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task DynamicQuest_ExpireEmpty_ReturnsOk()
    {
        InMemoryDynamicQuestRepository dynRepo = new();
        CommandDispatcher commands = Commands(new ExpireDynamicQuestsHandler(QuestService(dynRepo)));

        CommandResult result = await commands.DispatchAsync(new ExpireDynamicQuestsCommand());

        Assert.That(result.Success, Is.True);
        _eventBusMock.Verify(b => b.PublishAsync(
            It.Is<CommandExecutedEvent<ExpireDynamicQuestsCommand>>(e => e.Result.Success),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
