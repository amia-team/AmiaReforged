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
        return new DynamicQuestService(dynRepo, sessions, bus);
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
    public async Task DynamicQuest_Post_SuccessfulPost_PersistsAndPublishesThroughDispatcher()
    {
        InMemoryDynamicQuestRepository dynRepo = new();
        InMemoryEventBus bus = new();

        DynamicQuestTemplate template = new()
        {
            TemplateId = TemplateId.NewId(),
            Title = "Goblin Scouts",
            Description = "Clear the goblin camp.",
            CreatedAt = DateTime.UtcNow,
        };
        await dynRepo.SaveTemplateAsync(template);

        // Share one bus instance between the service and the dispatcher so the test
        // observes both the domain event and the generic command-executed event.
        CommandDispatcher commands = new(
            new[] { new PostDynamicQuestHandler(QuestService(dynRepo, bus)) },
            bus);

        CommandResult result = await commands.DispatchAsync(new PostDynamicQuestCommand
        {
            TemplateId = template.TemplateId.Value,
            PostedBy = _characterId
        });

        // 1. Success through the real dispatcher.
        Assert.That(result.Success, Is.True);

        // 2. Result carries the created posting ID.
        Assert.That(result.Data, Is.Not.Null, "Result should carry posting data");
        object? postingValue = result.Data!["postingId"];
        Assert.That(postingValue, Is.Not.Null, "Result should carry postingId");
        PostingId postingId = new(Guid.Parse(postingValue.ToString()!));

        // 3. The posting is persisted and matches the returned ID.
        DynamicQuestPosting? posting = await dynRepo.GetPostingAsync(postingId);
        Assert.That(posting, Is.Not.Null);
        Assert.That(posting!.PostingId, Is.EqualTo(postingId));

        // 4. The persisted posting is bound to the source template.
        Assert.That(posting.SourceTemplateId, Is.EqualTo(template.TemplateId));

        Assert.That(posting.PostedAt, Is.GreaterThan(DateTime.UnixEpoch));

        // 5. Exactly one QuestPostedEvent is observable on the bus.
        IReadOnlyList<QuestPostedEvent> postedEvents = bus.PublishedEvents.OfType<QuestPostedEvent>().ToList();
        Assert.That(postedEvents, Has.Count.EqualTo(1));

        // 6. The event carries the actor, posting and template.
        QuestPostedEvent posted = postedEvents.Single();
        Assert.That(posted.CharacterId, Is.EqualTo(_characterId));
        Assert.That(posted.PostingId, Is.EqualTo(postingId));
        Assert.That(posted.TemplateId, Is.EqualTo(template.TemplateId));
        Assert.That(posted.OccurredAt, Is.GreaterThan(DateTime.UnixEpoch));

        // 7. Exactly one successful generic command-executed event is observable.
        IReadOnlyList<CommandExecutedEvent<PostDynamicQuestCommand>> executedEvents =
            bus.PublishedEvents.OfType<CommandExecutedEvent<PostDynamicQuestCommand>>().ToList();
        Assert.That(executedEvents, Has.Count.EqualTo(1));
        Assert.That(executedEvents[0].Result.Success, Is.True);

        // 8. Posting a quest does not create a PlayerCodex entry.
        PlayerCodex? codex = await _codexRepo.LoadAsync(_characterId);
        Assert.That(codex, Is.Null, "Publishing a posting must not create a PlayerCodex entry");
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

    /// <summary>
    /// Bounded poll for the Codex mutation that arrives asynchronously from
    /// <see cref="CodexEventProcessor"/>'s own channel. Never sleeps a fixed multi-second
    /// duration; it returns as soon as the event has been applied.
    /// </summary>
    private static async Task<PlayerCodex?> WaitUntilCodexQuestAsync(
        InMemoryPlayerCodexRepository repository, CharacterId characterId, QuestId questId)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            PlayerCodex? codex = await repository.LoadAsync(characterId);
            if (codex is not null && codex.HasQuest(questId))
                return codex;
            await Task.Delay(10);
        }

        return await repository.LoadAsync(characterId);
    }

    [Test]
    public async Task DynamicQuest_Claim_SuccessfulClaim_PersistsSessionEventAndCodexThroughDispatcher()
    {
        InMemoryDynamicQuestRepository dynRepo = new();
        InMemoryPlayerCodexRepository codexRepo = new();
        InMemoryEventBus bus = new();

        // Minimal valid active template: no posting expiry, no claimant time limit,
        // no cooldown, unlimited completions, and unlimited simultaneous claim capacity.
        DynamicQuestTemplate template = new()
        {
            TemplateId = TemplateId.NewId(),
            Title = "Goblin Scouts",
            Description = "Clear the goblin camp.",
            CreatedAt = DateTime.UtcNow,
        };
        await dynRepo.SaveTemplateAsync(template);

        // Publish the posting through the real dispatcher so we have a live posting ID.
        CommandDispatcher poster = new(
            new[] { new PostDynamicQuestHandler(QuestService(dynRepo, bus)) },
            bus);

        CommandResult posted = await poster.DispatchAsync(new PostDynamicQuestCommand
        {
            TemplateId = template.TemplateId.Value,
            PostedBy = _characterId
        });
        Assert.That(posted.Success, Is.True);
        Assert.That(posted.Data, Is.Not.Null, "Posting result should carry postingId data");
        object? postingValue = posted.Data!["postingId"];
        Assert.That(postingValue, Is.Not.Null);
        PostingId postingId = new(Guid.Parse(postingValue.ToString()!));

        // Reset the bus so the claim-phase event counts are unambiguous.
        bus.ClearPublishedEvents();

        // A fresh claimant that did not post the quest.
        CharacterId claimant = CharacterId.New();

        // Wire the production event path:
        // service -> InMemoryEventBus -> forwarder -> CodexEventProcessor -> PlayerCodex.
        // The claim path only emits QuestClaimedEvent, so that is the only forwarded event
        // this test subscribes to — the forwarder remains the sole path into CodexEventProcessor.
        ObjectiveEvaluatorRegistry registry = new([]);
        QuestSessionManager sessions = new(registry);

        CodexEventProcessor processor = new(codexRepo);
        DynamicQuestCodexEventForwarder forwarder = new(processor);

        bus.Subscribe<QuestClaimedEvent>((@event, ct) => forwarder.HandleAsync(@event, ct));

        DynamicQuestService service = new(dynRepo, sessions, bus);
        CommandDispatcher commands = new(
            new[] { new ClaimDynamicQuestHandler(service) },
            bus);

        // Act: claim through the real dispatcher.
        CommandResult claim = await commands.DispatchAsync(new ClaimDynamicQuestCommand
        {
            CharacterId = claimant,
            PostingId = postingId.Value
        });

        // 1. Claim command succeeds.
        Assert.That(claim.Success, Is.True);

        // 2. Result contains a non-empty generated questId.
        Assert.That(claim.Data, Is.Not.Null, "Claim result should carry questId data");
        object? questValue = claim.Data!["questId"];
        Assert.That(questValue, Is.Not.Null);
        Assert.That(questValue.ToString(), Is.Not.Empty);
        QuestId questId = new(questValue.ToString()!);

        // 3. The posting now reports HasClaim(claimant) == true.
        DynamicQuestPosting? posting = await dynRepo.GetPostingAsync(postingId);
        Assert.That(posting, Is.Not.Null);
        Assert.That(posting!.HasClaim(claimant), Is.True);

        // 4. A runtime quest session exists for the claimant.
        Assert.That(sessions.HasSession(claimant, questId), Is.True);

        // 5. Exactly one matching QuestClaimedEvent was published on the bus.
        IReadOnlyList<QuestClaimedEvent> claimedEvents = bus.PublishedEvents.OfType<QuestClaimedEvent>().ToList();
        Assert.That(claimedEvents, Has.Count.EqualTo(1));

        // 6. Exactly one successful CommandExecutedEvent<ClaimDynamicQuestCommand>.
        IReadOnlyList<CommandExecutedEvent<ClaimDynamicQuestCommand>> executedEvents =
            bus.PublishedEvents.OfType<CommandExecutedEvent<ClaimDynamicQuestCommand>>().ToList();
        Assert.That(executedEvents, Has.Count.EqualTo(1));
        Assert.That(executedEvents[0].Result.Success, Is.True);

        // 7. After bounded waiting, the claimant's PlayerCodex exists and has the quest.
        PlayerCodex? codex = await WaitUntilCodexQuestAsync(codexRepo, claimant, questId);
        Assert.That(codex, Is.Not.Null,
            "The QuestClaimedEvent must reach the Codex through the bus forwarder after bounded waiting");

        // 8. The Codex contains exactly one quest with the generated QuestId.
        Assert.That(codex!.Quests, Has.Count.EqualTo(1));
        Assert.That(codex.HasQuest(questId), Is.True);

        // 9. The Codex entry carries the template/posting metadata and InProgress state.
        CodexQuestEntry entry = codex.GetQuest(questId)!;
        Assert.That(entry.Title, Is.EqualTo(template.Title));
        Assert.That(entry.Description, Is.EqualTo(template.Description));
        Assert.That(entry.SourceTemplateId, Is.EqualTo(template.TemplateId));
        Assert.That(entry.State, Is.EqualTo(QuestState.InProgress));

        // 10. No duplicate entry for the same QuestId.
        Assert.That(codex.Quests.Count(q => q.QuestId == questId), Is.EqualTo(1));
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
