using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// BDD-style tests for Traits CQRS implementation.
/// Tests demonstrate command/query pattern without NWN dependencies.
/// </summary>
[TestFixture]
public class TraitsCqrsTests
{
    private ICharacterTraitRepository _characterTraitRepository = null!;
    private ITraitRepository _traitRepository = null!;
    private IEventBus _eventBus = null!;
    private List<IDomainEvent> _publishedEvents = null!;

    private SelectTraitCommandHandler _selectHandler = null!;
    private DeselectTraitCommandHandler _deselectHandler = null!;
    private ConfirmTraitsCommandHandler _confirmHandler = null!;
    private UnlockTraitCommandHandler _unlockHandler = null!;
    private SetTraitActiveCommandHandler _setActiveHandler = null!;

    private GetCharacterTraitsQueryHandler _getTraitsHandler = null!;
    private GetTraitBudgetQueryHandler _getBudgetHandler = null!;
    private GetTraitDefinitionQueryHandler _getDefinitionHandler = null!;
    private GetAllTraitsQueryHandler _getAllTraitsHandler = null!;

    private CharacterId _testCharacterId;
    private const string BraveTraitTag = "brave";
    private const string CowardTraitTag = "coward";
    private const string HeroTraitTag = "hero";

    [SetUp]
    public void SetUp()
    {
        _characterTraitRepository = new InMemoryCharacterTraitRepository();
        _traitRepository = new InMemoryTraitRepository();
        _publishedEvents = new List<IDomainEvent>();
        _eventBus = new TestEventBus(_publishedEvents);

        // Initialize command handlers
        _selectHandler = new SelectTraitCommandHandler(_characterTraitRepository, _traitRepository, _eventBus);
        _deselectHandler = new DeselectTraitCommandHandler(_characterTraitRepository, _eventBus);
        _confirmHandler = new ConfirmTraitsCommandHandler(_characterTraitRepository, _traitRepository, _eventBus);
        _unlockHandler = new UnlockTraitCommandHandler(_traitRepository, _eventBus);
        _setActiveHandler = new SetTraitActiveCommandHandler(_characterTraitRepository, _eventBus);

        // Initialize query handlers
        _getTraitsHandler = new GetCharacterTraitsQueryHandler(_characterTraitRepository);
        _getBudgetHandler = new GetTraitBudgetQueryHandler(_characterTraitRepository, _traitRepository);
        _getDefinitionHandler = new GetTraitDefinitionQueryHandler(_traitRepository);
        _getAllTraitsHandler = new GetAllTraitsQueryHandler(_traitRepository);

        _testCharacterId = CharacterId.From(Guid.NewGuid());

        // Add test trait definitions
        _traitRepository.Add(new Trait
        {
            Tag = BraveTraitTag,
            Name = "Brave",
            Description = "Fearless in combat",
            PointCost = 1
        });

        _traitRepository.Add(new Trait
        {
            Tag = CowardTraitTag,
            Name = "Coward",
            Description = "Easily frightened",
            PointCost = -1 // Negative cost grants points
        });

        _traitRepository.Add(new Trait
        {
            Tag = HeroTraitTag,
            Name = "Hero",
            Description = "Legendary figure",
            PointCost = 2,
            RequiresUnlock = true
        });
    }

    #region Select Trait Tests

    [Test]
    public async Task SelectTrait_WithValidTrait_ShouldSucceed()
    {
        // Given
        Dictionary<string, bool> unlockedTraits = new();
        SelectTraitCommand command = new(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits);

        // When
        CommandResult result = await _selectHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!["characterTraitId"], Is.TypeOf<Guid>());

        // And the trait should be in repository
        List<CharacterTrait> traits = await _getTraitsHandler.HandleAsync(
            new GetCharacterTraitsQuery(_testCharacterId),
            CancellationToken.None);
        Assert.That(traits, Has.Count.EqualTo(1));
        Assert.That(traits[0].TraitTag.Value, Is.EqualTo(BraveTraitTag));
        Assert.That(traits[0].IsConfirmed, Is.False);

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        TraitSelectedEvent? evt = _publishedEvents[0] as TraitSelectedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.TraitTag.Value, Is.EqualTo(BraveTraitTag));
    }

    [Test]
    public async Task SelectTrait_WithNonexistentTrait_ShouldFail()
    {
        // Given
        Dictionary<string, bool> unlockedTraits = new();
        SelectTraitCommand command = new(_testCharacterId, new TraitTag("nonexistent"), unlockedTraits);

        // When
        CommandResult result = await _selectHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task SelectTrait_WithAlreadySelected_ShouldFail()
    {
        // Given - trait already selected
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When - trying to select again
        CommandResult result = await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("already selected"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task SelectTrait_RequiringUnlock_WithoutUnlock_ShouldFail()
    {
        // Given - trait requires unlock but is not unlocked
        Dictionary<string, bool> unlockedTraits = new();
        SelectTraitCommand command = new(_testCharacterId, new TraitTag(HeroTraitTag), unlockedTraits);

        // When
        CommandResult result = await _selectHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("requires unlock"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task SelectTrait_RequiringUnlock_WithUnlock_ShouldSucceed()
    {
        // Given - trait is unlocked
        Dictionary<string, bool> unlockedTraits = new() { [HeroTraitTag] = true };
        SelectTraitCommand command = new(_testCharacterId, new TraitTag(HeroTraitTag), unlockedTraits);

        // When
        CommandResult result = await _selectHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        List<CharacterTrait> traits = await _getTraitsHandler.HandleAsync(
            new GetCharacterTraitsQuery(_testCharacterId),
            CancellationToken.None);
        Assert.That(traits, Has.Count.EqualTo(1));
        Assert.That(traits[0].TraitTag.Value, Is.EqualTo(HeroTraitTag));
        Assert.That(traits[0].IsUnlocked, Is.True);
    }

    #endregion

    #region Deselect Trait Tests

    [Test]
    public async Task DeselectTrait_WithUnconfirmedTrait_ShouldSucceed()
    {
        // Given - unconfirmed trait
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When
        DeselectTraitCommand command = new(_testCharacterId, new TraitTag(BraveTraitTag));
        CommandResult result = await _deselectHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        // And trait should be removed
        List<CharacterTrait> traits = await _getTraitsHandler.HandleAsync(
            new GetCharacterTraitsQuery(_testCharacterId),
            CancellationToken.None);
        Assert.That(traits, Is.Empty);

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        TraitDeselectedEvent? evt = _publishedEvents[0] as TraitDeselectedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.TraitTag.Value, Is.EqualTo(BraveTraitTag));
    }

    [Test]
    public async Task DeselectTrait_WithNonexistentTrait_ShouldFail()
    {
        // Given - no traits selected
        DeselectTraitCommand command = new(_testCharacterId, new TraitTag(BraveTraitTag));

        // When
        CommandResult result = await _deselectHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not selected"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task DeselectTrait_WithConfirmedTrait_ShouldFail()
    {
        // Given - confirmed trait
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        await _confirmHandler.HandleAsync(
            new ConfirmTraitsCommand(_testCharacterId),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When - trying to deselect confirmed trait
        DeselectTraitCommand command = new(_testCharacterId, new TraitTag(BraveTraitTag));
        CommandResult result = await _deselectHandler.HandleAsync(command, CancellationToken.None);

        // Then - should fail (confirmed traits are permanent)
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("confirmed"));
        Assert.That(result.ErrorMessage, Does.Contain("cannot be deselected"));

        // And trait should still be present
        List<CharacterTrait> traits = await _getTraitsHandler.HandleAsync(
            new GetCharacterTraitsQuery(_testCharacterId),
            CancellationToken.None);
        Assert.That(traits, Has.Count.EqualTo(1));
        Assert.That(traits[0].IsConfirmed, Is.True);
    }

    #endregion

    #region Confirm Traits Tests

    [Test]
    public async Task ConfirmTraits_WithValidBudget_ShouldSucceed()
    {
        // Given - select one trait (costs 1 point, within budget of 2)
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When
        ConfirmTraitsCommand command = new(_testCharacterId);
        CommandResult result = await _confirmHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!["confirmedCount"], Is.EqualTo(1));

        // And traits should be confirmed
        List<CharacterTrait> traits = await _getTraitsHandler.HandleAsync(
            new GetCharacterTraitsQuery(_testCharacterId),
            CancellationToken.None);
        Assert.That(traits[0].IsConfirmed, Is.True);

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        TraitsConfirmedEvent? evt = _publishedEvents[0] as TraitsConfirmedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.TraitCount, Is.EqualTo(1));
        Assert.That(evt.FinalBudgetAvailable, Is.EqualTo(1)); // 2 base - 1 spent
    }

    [Test]
    public async Task ConfirmTraits_WithNegativeBudget_ShouldFail()
    {
        // Given - select traits costing 3 points (over budget of 2)
        Dictionary<string, bool> unlockedTraits = new() { [HeroTraitTag] = true };
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(HeroTraitTag), unlockedTraits),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When
        ConfirmTraitsCommand command = new(_testCharacterId);
        CommandResult result = await _confirmHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("negative"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task ConfirmTraits_WithNegativeTraitGrantingPoints_ShouldSucceed()
    {
        // Given - select positive and negative traits (net 0 points spent)
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(CowardTraitTag), unlockedTraits),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When
        ConfirmTraitsCommand command = new(_testCharacterId);
        CommandResult result = await _confirmHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        TraitsConfirmedEvent? evt = _publishedEvents[0] as TraitsConfirmedEvent;
        Assert.That(evt!.FinalBudgetAvailable, Is.EqualTo(2)); // 2 base + 1 (from Coward) - 1 (for Brave)
    }

    #endregion

    #region Unlock Trait Tests

    [Test]
    public async Task UnlockTrait_WithValidTrait_ShouldPublishEvent()
    {
        // Given
        UnlockTraitCommand command = new(_testCharacterId, new TraitTag(HeroTraitTag));

        // When
        CommandResult result = await _unlockHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        TraitUnlockedEvent? evt = _publishedEvents[0] as TraitUnlockedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.TraitTag.Value, Is.EqualTo(HeroTraitTag));
    }

    [Test]
    public async Task UnlockTrait_WithNonexistentTrait_ShouldFail()
    {
        // Given
        UnlockTraitCommand command = new(_testCharacterId, new TraitTag("nonexistent"));

        // When
        CommandResult result = await _unlockHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    #endregion

    #region Set Trait Active Tests

    [Test]
    public async Task SetTraitActive_ToInactive_ShouldSucceed()
    {
        // Given - selected trait
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When
        SetTraitActiveCommand command = new(_testCharacterId, new TraitTag(BraveTraitTag), false);
        CommandResult result = await _setActiveHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        List<CharacterTrait> traits = await _getTraitsHandler.HandleAsync(
            new GetCharacterTraitsQuery(_testCharacterId),
            CancellationToken.None);
        Assert.That(traits[0].IsActive, Is.False);

        TraitActiveStateChangedEvent? evt = _publishedEvents[0] as TraitActiveStateChangedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.IsActive, Is.False);
    }

    [Test]
    public async Task SetTraitActive_WithNonexistentTrait_ShouldFail()
    {
        // Given - no traits selected
        SetTraitActiveCommand command = new(_testCharacterId, new TraitTag(BraveTraitTag), false);

        // When
        CommandResult result = await _setActiveHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not selected"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task SetTraitActive_ToSameState_ShouldFail()
    {
        // Given - active trait
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        _publishedEvents.Clear();

        // When - trying to set to active again
        SetTraitActiveCommand command = new(_testCharacterId, new TraitTag(BraveTraitTag), true);
        CommandResult result = await _setActiveHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("already active"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    #endregion

    #region Query Tests

    [Test]
    public async Task GetCharacterTraits_WithMultipleTraits_ShouldReturnAll()
    {
        // Given - multiple selected traits
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(CowardTraitTag), unlockedTraits),
            CancellationToken.None);

        // When
        GetCharacterTraitsQuery query = new(_testCharacterId);
        List<CharacterTrait> traits = await _getTraitsHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(traits, Has.Count.EqualTo(2));
        Assert.That(traits.Select(t => t.TraitTag.Value), Contains.Item(BraveTraitTag));
        Assert.That(traits.Select(t => t.TraitTag.Value), Contains.Item(CowardTraitTag));
    }

    [Test]
    public async Task GetTraitBudget_WithNoTraits_ShouldReturnDefault()
    {
        // Given - no traits
        GetTraitBudgetQuery query = new(_testCharacterId);

        // When
        TraitBudget budget = await _getBudgetHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(budget.BasePoints, Is.EqualTo(2));
        Assert.That(budget.SpentPoints, Is.EqualTo(0));
        Assert.That(budget.AvailablePoints, Is.EqualTo(2));
    }

    [Test]
    public async Task GetTraitBudget_WithSelectedTraits_ShouldCalculateCorrectly()
    {
        // Given - selected traits
        Dictionary<string, bool> unlockedTraits = new();
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(BraveTraitTag), unlockedTraits),
            CancellationToken.None);
        await _selectHandler.HandleAsync(
            new SelectTraitCommand(_testCharacterId, new TraitTag(CowardTraitTag), unlockedTraits),
            CancellationToken.None);

        // When
        GetTraitBudgetQuery query = new(_testCharacterId);
        TraitBudget budget = await _getBudgetHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(budget.SpentPoints, Is.EqualTo(0)); // 1 (Brave) - 1 (Coward)
        Assert.That(budget.AvailablePoints, Is.EqualTo(2));
    }

    [Test]
    public async Task GetTraitDefinition_WithExistingTrait_ShouldReturnTrait()
    {
        // Given
        GetTraitDefinitionQuery query = new(new TraitTag(BraveTraitTag));

        // When
        Trait? trait = await _getDefinitionHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(trait, Is.Not.Null);
        Assert.That(trait!.Tag, Is.EqualTo(BraveTraitTag));
        Assert.That(trait.PointCost, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTraitDefinition_WithNonexistent_ShouldReturnNull()
    {
        // Given
        GetTraitDefinitionQuery query = new(new TraitTag("nonexistent"));

        // When
        Trait? trait = await _getDefinitionHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(trait, Is.Null);
    }

    [Test]
    public async Task GetAllTraits_ShouldReturnAllDefinitions()
    {
        // Given
        GetAllTraitsQuery query = new();

        // When
        List<Trait> traits = await _getAllTraitsHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(traits, Has.Count.EqualTo(3));
        Assert.That(traits.Select(t => t.Tag), Contains.Item(BraveTraitTag));
        Assert.That(traits.Select(t => t.Tag), Contains.Item(CowardTraitTag));
        Assert.That(traits.Select(t => t.Tag), Contains.Item(HeroTraitTag));
    }

    #endregion

    #region Helper Classes

    private class TestEventBus : IEventBus
    {
        private readonly List<IDomainEvent> _events;

        public TestEventBus(List<IDomainEvent> events)
        {
            _events = events;
        }

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent
        {
            // Not needed for these tests
        }
    }

    #endregion
}

