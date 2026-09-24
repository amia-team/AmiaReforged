using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class AwardProficiencyCommandTests
{
    private static readonly Guid TestCharacterIdGuid = Guid.Parse("cccc2222-3333-4444-5555-666677778888");
    private static readonly CharacterId TestCharacterId = CharacterId.From(TestCharacterIdGuid);

    private InMemoryIndustryMembershipRepository _membershipRepository = null!;
    private ProficiencyProgressionService _proficiencyService = null!;
    private InMemoryEventBus _eventBus = null!;
    private CommandDispatcher _dispatcher = null!;
    private AwardProficiencyHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _membershipRepository = new InMemoryIndustryMembershipRepository();
        _proficiencyService = new ProficiencyProgressionService();
        _eventBus = new InMemoryEventBus();

        _dispatcher = new CommandDispatcher(
            new List<ICommandHandlerMarker> { new AwardProficiencyHandler(_membershipRepository, _proficiencyService, _dispatcher, _eventBus) },
            _eventBus);

        _handler = new AwardProficiencyHandler(_membershipRepository, _proficiencyService, _dispatcher, _eventBus);
    }

    private IndustryMembership CreateMembership(
        ProficiencyLevel level = ProficiencyLevel.Layman,
        int xpLevel = 1,
        int xp = 0)
    {
        IndustryMembership membership = new()
        {
            Id = Guid.NewGuid(),
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Level = level,
            ProficiencyXpLevel = xpLevel,
            ProficiencyXp = xp,
            CharacterKnowledge = []
        };
        _membershipRepository.Add(membership);
        return membership;
    }

    // ==================== XP rollover ====================

    [Test]
    public async Task Award_WhenPointsCrossLevelThreshold_ThenLevelsUpAndPersists()
    {
        IndustryMembership membership = CreateMembership(xpLevel: 1, xp: 0);
        int costForLevel1 = ProficiencyXpCurve.XpForLevel(1);

        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = costForLevel1
        });

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["proficiencyXpLevel"], Is.EqualTo(2));
        Assert.That((int)result.Data!["proficiencyLevelsGained"], Is.EqualTo(1));
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(2), "Membership should be persisted");
        Assert.That(membership.ProficiencyXp, Is.EqualTo(0));
    }

    [Test]
    public async Task Award_WhenPointsOverflowMultipleLevels_ThenCarriesRemainderAndPersists()
    {
        IndustryMembership membership = CreateMembership(xpLevel: 1, xp: 0);
        int costForLevel1 = ProficiencyXpCurve.XpForLevel(1);
        int costForLevel2 = ProficiencyXpCurve.XpForLevel(2);

        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = costForLevel1 + costForLevel2 + 50
        });

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["proficiencyXpLevel"], Is.EqualTo(3));
        Assert.That((int)result.Data!["proficiencyXpRemaining"], Is.EqualTo(50));
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(3));
        Assert.That(membership.ProficiencyXp, Is.EqualTo(50));
    }

    // ==================== Tier ceiling hard gate ====================

    [Test]
    public async Task Award_AtTierCeiling_IsBlockedAndPersistsNoChange()
    {
        // Novice ceiling is level 25 — set membership to level 25 as Novice (hasn't ranked up)
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice, xpLevel: 25, xp: 0);

        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = 500
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Rank up"));
        Assert.That((bool)result.Data!["proficiencyAtTierCeiling"], Is.True);
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(25), "Membership should not be persisted");
        Assert.That(membership.ProficiencyXp, Is.EqualTo(0));
    }

    [Test]
    public async Task Award_ApproachingTierCeiling_CapsAtCeilingAndPersistsCeiling()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice, xpLevel: 24, xp: 0);

        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = 100_000
        });

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["proficiencyXpLevel"], Is.EqualTo(25));
        Assert.That((bool)result.Data!["proficiencyAtTierCeiling"], Is.True);
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(25));
        Assert.That(membership.ProficiencyXp, Is.EqualTo(0));
    }

    // ==================== Missing membership and invalid XP ====================

    [Test]
    public async Task Award_MissingMembership_Fails()
    {
        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = 100
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not a member"));
    }

    [Test]
    public async Task Award_ZeroPoints_Fails()
    {
        CreateMembership();

        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = 0
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("zero or negative"));
    }

    [Test]
    public async Task Award_NegativePoints_Fails()
    {
        CreateMembership();

        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = -5
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("zero or negative"));
    }

    [Test]
    public async Task Award_AtMaxLevel_IsBlocked()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Grandmaster, xpLevel: 125, xp: 0);

        CommandResult result = await _handler.HandleAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = 500
        });

        Assert.That(result.Success, Is.False);
        Assert.That((int)result.Data!["proficiencyXpLevel"], Is.EqualTo(125));
    }

    // ==================== Generic command-executed event + domain event ====================

    [Test]
    public async Task Award_WhenDispatched_ThenPublishesGenericCommandExecutedEventAndDomainEvent()
    {
        IndustryMembership membership = CreateMembership(xpLevel: 1, xp: 0);
        int costForLevel1 = ProficiencyXpCurve.XpForLevel(1);

        CommandResult result = await _dispatcher.DispatchAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = costForLevel1
        });

        Assert.That(result.Success, Is.True);

        CommandExecutedEvent<AwardProficiencyCommand>? executed =
            _eventBus.PublishedEvents.OfType<CommandExecutedEvent<AwardProficiencyCommand>>().SingleOrDefault();
        executed.Should().NotBeNull();
        executed!.Command.Points.Should().Be(costForLevel1);
        executed.Result.Success.Should().BeTrue();

        ProficiencyXpAwardedEvent? gained =
            _eventBus.PublishedEvents.OfType<ProficiencyXpAwardedEvent>().SingleOrDefault();
        gained.Should().NotBeNull();
        gained!.MemberId.Should().Be(TestCharacterId);
        gained.NewLevel.Should().Be(2);
        gained.LevelsGained.Should().Be(1);
        gained.IndustryTag.Should().Be(new IndustryTag("test_industry"));
        // Membership was persisted by the handler.
        _membershipRepository.All(TestCharacterIdGuid).Single().ProficiencyXpLevel.Should().Be(2);
    }

    [Test]
    public async Task Award_AtTierCeiling_WhenDispatched_ThenNoDomainEventPublished()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice, xpLevel: 25, xp: 0);

        CommandResult result = await _dispatcher.DispatchAsync(new AwardProficiencyCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_industry"),
            Points = 500
        });

        Assert.That(result.Success, Is.False);
        _eventBus.PublishedEvents.OfType<ProficiencyXpAwardedEvent>().Should().BeEmpty();
    }
}
