using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;

/// <summary>
/// BDD-style integration tests for the Interaction Framework dispatcher.
/// Uses a stub <see cref="IInteractionHandler"/> to verify session lifecycle,
/// exclusive sessions, precondition checks, and event publication.
/// </summary>
[TestFixture]
public class InteractionFrameworkTests
{
    private IInteractionSessionManager _sessionManager = null!;
    private ICharacterRepository _characterRepository = null!;
    private IInteractionHandlerRegistry _handlerRegistry = null!;
    private PerformInteractionCommandHandler _handler = null!;

    private List<IDomainEvent> _publishedEvents = null!;
    private IEventBus _eventBus = null!;

    private StubInteractionHandler _stubHandler = null!;
    private CharacterId _characterId;

    [SetUp]
    public void SetUp()
    {
        _sessionManager = new InteractionSessionManager();
        _characterRepository = new RuntimeCharacterRepository();
        _publishedEvents = [];
        _eventBus = new TestEventBus(_publishedEvents);

        _stubHandler = new StubInteractionHandler();
        _handlerRegistry = new InteractionHandlerRegistry(new[] { (IInteractionHandler)_stubHandler });

        _handler = new PerformInteractionCommandHandler(
            _sessionManager, _characterRepository, _handlerRegistry, _eventBus);

        // Create a test character
        _characterId = CharacterId.New();
        var character = new TestCharacter(
            new Dictionary<EquipmentSlots, ItemSnapshot>(),
            [],
            _characterId,
            InMemoryCharacterKnowledgeRepository.Create(),
            null!);
        _characterRepository.Add(character);
    }

    #region Starting Interactions

    [Test]
    public async Task Starting_interaction_creates_session_and_publishes_event()
    {
        // Given a character and a known interaction handler
        Guid targetId = Guid.NewGuid();

        // When performing the interaction for the first time
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "test_interaction", targetId));

        // Then it should succeed
        result.Success.Should().BeTrue();

        // And an InteractionStartedEvent should be published
        _publishedEvents.OfType<InteractionStartedEvent>().Should().HaveCount(1);
        var started = _publishedEvents.OfType<InteractionStartedEvent>().First();
        started.CharacterId.Should().Be(_characterId.Value);
        started.InteractionTag.Should().Be("test_interaction");
        started.TargetId.Should().Be(targetId);
    }

    [Test]
    public async Task Starting_interaction_checks_preconditions()
    {
        // Given a handler that rejects preconditions
        _stubHandler.PreconditionOverride = PreconditionResult.Fail("Tool not equipped");

        // When trying to start the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "test_interaction", Guid.NewGuid()));

        // Then it should fail with the precondition message
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Tool not equipped");
    }

    [Test]
    public async Task Unknown_interaction_tag_returns_failure()
    {
        // When performing an interaction with an unknown tag
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "nonexistent", Guid.NewGuid()));

        // Then it should fail
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown interaction type");
    }

    [Test]
    public async Task Unknown_character_returns_failure()
    {
        // When performing an interaction with an unknown character
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(CharacterId.New(), "test_interaction", Guid.NewGuid()));

        // Then it should fail
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Character not found");
    }

    #endregion

    #region Progress and Completion

    [Test]
    public async Task Multi_round_interaction_reports_in_progress_until_complete()
    {
        // Given a handler requiring 3 rounds
        _stubHandler.RequiredRoundsOverride = 3;
        Guid targetId = Guid.NewGuid();
        PerformInteractionCommand command = new(_characterId, "test_interaction", targetId);

        // When ticking the first round (creates session + ticks)
        CommandResult result1 = await _handler.HandleAsync(command);
        result1.Success.Should().BeTrue();
        result1.Data!["status"].Should().Be("InProgress");

        // And ticking the second round
        CommandResult result2 = await _handler.HandleAsync(command);
        result2.Data!["status"].Should().Be("InProgress");

        // And ticking the third round (should complete)
        CommandResult result3 = await _handler.HandleAsync(command);
        result3.Success.Should().BeTrue();
        result3.Data!["status"].Should().Be("Completed");
    }

    [Test]
    public async Task Completing_interaction_publishes_completed_event()
    {
        // Given a single-round interaction
        _stubHandler.RequiredRoundsOverride = 1;
        Guid targetId = Guid.NewGuid();

        // When performing the interaction
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "test_interaction", targetId));

        // Then an InteractionCompletedEvent should be published
        _publishedEvents.OfType<InteractionCompletedEvent>().Should().HaveCount(1);
        var completed = _publishedEvents.OfType<InteractionCompletedEvent>().First();
        completed.Success.Should().BeTrue();
        completed.InteractionTag.Should().Be("test_interaction");
    }

    [Test]
    public async Task Session_is_removed_after_completion()
    {
        // Given a single-round interaction
        _stubHandler.RequiredRoundsOverride = 1;

        // When the interaction completes
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "test_interaction", Guid.NewGuid()));

        // Then no active session should remain
        _sessionManager.HasActiveSession(_characterId).Should().BeFalse();
    }

    #endregion

    #region Exclusive Sessions

    [Test]
    public async Task Starting_different_interaction_cancels_previous_session()
    {
        // Given an in-progress interaction
        _stubHandler.RequiredRoundsOverride = 5;
        Guid target1 = Guid.NewGuid();
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "test_interaction", target1));

        // When starting a different interaction (different target)
        _publishedEvents.Clear();
        Guid target2 = Guid.NewGuid();
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "test_interaction", target2));

        // Then the old session should be cancelled and a new one started
        _stubHandler.CancelCallCount.Should().Be(1);
        _sessionManager.GetActiveSession(_characterId)!.TargetId.Should().Be(target2);
    }

    [Test]
    public async Task Continuing_same_interaction_does_not_cancel()
    {
        // Given an in-progress interaction
        _stubHandler.RequiredRoundsOverride = 5;
        Guid targetId = Guid.NewGuid();
        PerformInteractionCommand command = new(_characterId, "test_interaction", targetId);

        await _handler.HandleAsync(command);

        // When continuing with the same command
        await _handler.HandleAsync(command);

        // Then no cancel should have been called
        _stubHandler.CancelCallCount.Should().Be(0);
    }

    #endregion

    #region Metadata Threading

    [Test]
    public async Task Command_metadata_flows_to_session()
    {
        // Given a command with metadata
        _stubHandler.RequiredRoundsOverride = 3;
        var metadata = new Dictionary<string, object> { ["allowedTypes"] = "Ore,Geode" };
        PerformInteractionCommand command = new(
            _characterId, "test_interaction", Guid.NewGuid(), "test_area", metadata);

        // When starting the interaction
        await _handler.HandleAsync(command);

        // Then the session should carry metadata
        InteractionSession? session = _sessionManager.GetActiveSession(_characterId);
        session.Should().NotBeNull();
        session!.AreaResRef.Should().Be("test_area");
        session.Metadata.Should().ContainKey("allowedTypes");
    }

    #endregion

    #region Test Doubles

    /// <summary>
    /// Configurable stub handler for testing the framework pipeline.
    /// </summary>
    private class StubInteractionHandler : IInteractionHandler
    {
        public string InteractionTag => "test_interaction";
        public InteractionTargetMode TargetMode => InteractionTargetMode.Node;

        public PreconditionResult? PreconditionOverride { get; set; }
        public int RequiredRoundsOverride { get; set; } = 1;
        public int CancelCallCount { get; private set; }

        public PreconditionResult CanStart(ICharacter character, InteractionContext context)
            => PreconditionOverride ?? PreconditionResult.Success();

        public int CalculateRequiredRounds(ICharacter character, InteractionContext context)
            => RequiredRoundsOverride;

        public TickResult OnTick(InteractionSession session, ICharacter character)
        {
            int newProgress = session.IncrementProgress(1);
            InteractionStatus status = session.IsComplete
                ? InteractionStatus.Completed
                : InteractionStatus.Active;
            return new TickResult(status, newProgress, session.RequiredRounds);
        }

        public Task<InteractionOutcome> OnCompleteAsync(
            InteractionSession session, ICharacter character, CancellationToken ct = default)
            => Task.FromResult(InteractionOutcome.Succeeded("Test completed"));

        public void OnCancel(InteractionSession session, ICharacter character)
            => CancelCallCount++;
    }

    private class TestEventBus : IEventBus
    {
        private readonly List<IDomainEvent> _events;
        public TestEventBus(List<IDomainEvent> events) => _events = events;

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
            where TEvent : IDomainEvent { }
    }

    #endregion
}
