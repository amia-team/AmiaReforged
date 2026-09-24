using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Tests;

[TestFixture]
public class GrantLevelUpKnowledgePointCommandTests
{
    private static readonly Guid TestCharacterIdGuid = Guid.Parse("cccc4444-5555-6666-7777-888899990000");
    private static readonly CharacterId TestCharacterId = CharacterId.From(TestCharacterIdGuid);

    private InMemoryKnowledgeProgressionRepository _progressionRepository = null!;
    private InMemoryEventBus _eventBus = null!;
    private CommandDispatcher _dispatcher = null!;
    private GrantLevelUpKnowledgePointHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _progressionRepository = InMemoryKnowledgeProgressionRepository.Create();
        _eventBus = new InMemoryEventBus();

        _dispatcher = new CommandDispatcher(
            new List<ICommandHandlerMarker> { new GrantLevelUpKnowledgePointHandler(_progressionRepository) },
            _eventBus);

        _handler = new GrantLevelUpKnowledgePointHandler(_progressionRepository);
    }

    private KnowledgeProgression SeedProgression(int levelUpKp = 0, int economyKp = 0, int accumulated = 0)
    {
        KnowledgeProgression progression = new()
        {
            CharacterId = TestCharacterIdGuid,
            EconomyEarnedKnowledgePoints = economyKp,
            LevelUpKnowledgePoints = levelUpKp,
            AccumulatedProgressionPoints = accumulated
        };
        _progressionRepository.Add(progression);
        return progression;
    }

    // ==================== Basic grant ====================

    [Test]
    public async Task Grant_WhenCharacterHasNoProgression_ThenCreatesAndGrantsExactlyOneLevelUpKp()
    {
        // Given: no progression row exists.
        SeedProgression();

        GrantLevelUpKnowledgePointCommand command = new(TestCharacterId);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        // Exactly one level-up KP granted.
        Assert.That((int)result.Data!["levelUpKnowledgePoints"], Is.EqualTo(1));
        // Total KP reflects the single free bonus; the economy curve is untouched.
        Assert.That((int)result.Data!["totalKnowledgePoints"], Is.EqualTo(1));
        // Economy KP and accumulated progression points are never modified.
        Assert.That((int)result.Data!["economyKnowledgePoints"], Is.EqualTo(0));
        Assert.That((int)result.Data!["accumulatedProgressionPoints"], Is.EqualTo(0));
    }

    [Test]
    public async Task Grant_WhenCharacterHasProgression_ThenOnlyLevelUpCounterIncrements()
    {
        // Given: character already has economy KP and accumulated points.
        KnowledgeProgression progression = SeedProgression(levelUpKp: 2, economyKp: 5, accumulated: 85);

        CommandResult result = await _handler.HandleAsync(new GrantLevelUpKnowledgePointCommand(TestCharacterId));

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["levelUpKnowledgePoints"], Is.EqualTo(3));
        // Economy KP and accumulated points are not affected by a level-up grant.
        Assert.That((int)result.Data!["economyKnowledgePoints"], Is.EqualTo(5));
        Assert.That((int)result.Data!["accumulatedProgressionPoints"], Is.EqualTo(85));
        Assert.That((int)result.Data!["totalKnowledgePoints"], Is.EqualTo(8));
    }

    // ==================== Cap enforcement ====================

    [Test]
    public async Task Grant_WhenAtLevelUpCap_ThenRejectedAndNoMutation()
    {
        // Given: character already at the 30 level-up KP cap.
        KnowledgeProgression progression = SeedProgression(levelUpKp: 30, economyKp: 4, accumulated: 10);
        int economyBefore = progression.EconomyEarnedKnowledgePoints;
        int accumulatedBefore = progression.AccumulatedProgressionPoints;

        CommandResult result = await _handler.HandleAsync(new GrantLevelUpKnowledgePointCommand(TestCharacterId));

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("maximum"));
        // No mutation: economy KP and accumulated points unchanged.
        KnowledgeProgression after = _progressionRepository.GetByCharacterId(TestCharacterIdGuid)!;
        Assert.That(after.EconomyEarnedKnowledgePoints, Is.EqualTo(economyBefore));
        Assert.That(after.AccumulatedProgressionPoints, Is.EqualTo(accumulatedBefore));
        Assert.That(after.LevelUpKnowledgePoints, Is.EqualTo(30));
        // No mutation: the repository's Update() was never called on the rejected path.
        Assert.That(_progressionRepository.UpdateCount, Is.EqualTo(0));
    }

    // ==================== Generic command-executed event ====================

    [Test]
    public async Task Grant_WhenDispatchedSuccessfully_ThenPublishesGenericCommandExecutedEvent()
    {
        SeedProgression();

        CommandResult result = await _dispatcher.DispatchAsync(new GrantLevelUpKnowledgePointCommand(TestCharacterId));

        Assert.That(result.Success, Is.True);

        CommandExecutedEvent<GrantLevelUpKnowledgePointCommand>? executed =
            _eventBus.PublishedEvents.OfType<CommandExecutedEvent<GrantLevelUpKnowledgePointCommand>>().SingleOrDefault();
        executed.Should().NotBeNull();
        executed!.Command.CharacterId.Should().Be(TestCharacterId);
        executed.Result.Success.Should().BeTrue();

        // The generic event is the only published event; the level-up grant publishes no
        // domain event of its own (it is a deliberate bypass of the economy curve).
        Assert.That(_eventBus.PublishedEvents.Count, Is.EqualTo(1));
        Assert.That(_eventBus.PublishedEvents[0], Is.InstanceOf<CommandExecutedEvent<GrantLevelUpKnowledgePointCommand>>());
    }

}
