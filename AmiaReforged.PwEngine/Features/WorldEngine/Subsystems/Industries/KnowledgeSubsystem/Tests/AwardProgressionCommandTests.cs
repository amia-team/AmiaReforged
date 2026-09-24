using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Tests;

[TestFixture]
public class AwardProgressionCommandTests
{
    private static readonly Guid TestCharacterIdGuid = Guid.Parse("bbbb2222-3333-4444-5555-666677778888");
    private static readonly CharacterId TestCharacterId = CharacterId.From(TestCharacterIdGuid);

    private InMemoryKnowledgeProgressionRepository _progressionRepository = null!;
    private InMemoryKnowledgeCapProfileRepository _capProfileRepository = null!;
    private TestWorldConfigProvider _configProvider = null!;
    private InMemoryEventBus _eventBus = null!;
    private CommandDispatcher _dispatcher = null!;
    private AwardProgressionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _progressionRepository = new InMemoryKnowledgeProgressionRepository();
        _capProfileRepository = new InMemoryKnowledgeCapProfileRepository();
        _configProvider = new TestWorldConfigProvider();
        _eventBus = new InMemoryEventBus();

        KnowledgeProgressionService service = new(
            _progressionRepository, _capProfileRepository, _configProvider, _eventBus);

        _dispatcher = new CommandDispatcher(
            new List<ICommandHandlerMarker> { new AwardProgressionHandler(service, _dispatcher) },
            _eventBus);

        _handler = new AwardProgressionHandler(service, _dispatcher);
    }

    private KnowledgeProgression SeedProgression(int economyKp = 0, int accumulated = 0)
    {
        KnowledgeProgression progression = new()
        {
            CharacterId = TestCharacterIdGuid,
            EconomyEarnedKnowledgePoints = economyKp,
            AccumulatedProgressionPoints = accumulated
        };
        _progressionRepository.Add(progression);
        return progression;
    }

    // ==================== Threshold rollover ====================

    [Test]
    public async Task Award_WhenAccumulatedPointsCrossMultipleThresholds_ThenRollsOverAndCarriesRemainder()
    {
        // Given: defaults (BaseCost=100, Exponential 1.15, soft=100, hard=150)
        // Awarding 300 points: KP#1 costs 100, KP#2 costs 115, leaving 85 accumulated.
        SeedProgression();

        CommandResult result = await _handler.HandleAsync(new AwardProgressionCommand
        {
            CharacterId = TestCharacterId,
            Points = 300
        });

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["knowledgePointsEarned"], Is.EqualTo(2));
        Assert.That((int)result.Data!["newEconomyKnowledgePointTotal"], Is.EqualTo(2));
        Assert.That((int)result.Data!["progressionPointsRemaining"], Is.EqualTo(85));
        Assert.That((int)result.Data!["progressionPointsRequired"], Is.EqualTo(133));
    }

    [Test]
    public async Task Award_WhenPointsBelowFirstThreshold_ThenNoKpAndPointsAccumulate()
    {
        SeedProgression();

        CommandResult result = await _handler.HandleAsync(new AwardProgressionCommand
        {
            CharacterId = TestCharacterId,
            Points = 50
        });

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["knowledgePointsEarned"], Is.EqualTo(0));
        Assert.That((int)result.Data!["newEconomyKnowledgePointTotal"], Is.EqualTo(0));
        Assert.That((int)result.Data!["progressionPointsRemaining"], Is.EqualTo(50));
    }

    // ==================== Soft-cap behavior ====================

    [Test]
    public async Task Award_WhenReachingSoftCap_ThenReportsAtSoftCap()
    {
        // Soft cap lowered to 2 via config. KP#1 costs 100, KP#2 costs 115.
        _configProvider.SetInt(WorldConstants.KnowledgePointDefaultSoftCap, 2);
        SeedProgression();

        CommandResult result = await _handler.HandleAsync(new AwardProgressionCommand
        {
            CharacterId = TestCharacterId,
            Points = 250
        });

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["knowledgePointsEarned"], Is.EqualTo(2));
        Assert.That((bool)result.Data!["isAtSoftCap"], Is.True);
        Assert.That((bool)result.Data!["isAtHardCap"], Is.False);
    }

    // ==================== Hard-cap rejection ====================

    [Test]
    public async Task Award_WhenAlreadyAtHardCap_ThenRejectionAndNoKp()
    {
        // Hard cap is 150 by default.
        SeedProgression(economyKp: 150, accumulated: 0);

        CommandResult result = await _handler.HandleAsync(new AwardProgressionCommand
        {
            CharacterId = TestCharacterId,
            Points = 100
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("hard cap"));
        Assert.That((int)result.Data!["knowledgePointsEarned"], Is.EqualTo(0));
        // The service returns a fresh Blocked result (NewEconomyKnowledgePointTotal = 0);
        // it does not echo the pre-existing total.
        Assert.That((int)result.Data!["newEconomyKnowledgePointTotal"], Is.EqualTo(0));
    }

    [Test]
    public async Task Award_ZeroPoints_Fails()
    {
        SeedProgression();

        CommandResult result = await _handler.HandleAsync(new AwardProgressionCommand
        {
            CharacterId = TestCharacterId,
            Points = 0
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("zero or negative"));
    }

    // ==================== Generic command-executed event ====================

    [Test]
    public async Task Award_WhenDispatched_ThenPublishesGenericCommandExecutedEvent()
    {
        SeedProgression();

        CommandResult result = await _dispatcher.DispatchAsync(new AwardProgressionCommand
        {
            CharacterId = TestCharacterId,
            Points = 100
        });

        Assert.That(result.Success, Is.True);

        CommandExecutedEvent<AwardProgressionCommand>? executed =
            _eventBus.PublishedEvents.OfType<CommandExecutedEvent<AwardProgressionCommand>>().SingleOrDefault();
        executed.Should().NotBeNull();
        executed!.Command.Points.Should().Be(100);
        executed.Result.Success.Should().BeTrue();

        // The domain event is also published by the service.
        KnowledgePointEarnedEvent? earned =
            _eventBus.PublishedEvents.OfType<KnowledgePointEarnedEvent>().SingleOrDefault();
        earned.Should().NotBeNull();
        earned!.NewEconomyKnowledgePointTotal.Should().Be(1);
    }

    // ==================== Crafting awards exactly once ====================

    [Test]
    public async Task Craft_WhenSuccessful_ThenAwardsProgressionPointsExactlyOnce()
    {
        Industry industry = new()
        {
            Tag = "test_crafting",
            Name = "Test Crafting",
            Knowledge = [],
            Recipes =
            [
                new Recipe
                {
                    RecipeId = new RecipeId("test_item"),
                    Name = "Test Item",
                    IndustryTag = new IndustryTag("test_crafting"),
                    RequiredKnowledge = [],
                    Ingredients = [],
                    Products = [],
                    ProgressionPointsAwarded = 250
                }
            ]
        };

        _progressionRepository.Add(new KnowledgeProgression
        {
            CharacterId = TestCharacterIdGuid
        });

        var membershipRepository = new InMemoryIndustryMembershipRepository();
        membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_crafting"),
            Level = ProficiencyLevel.Layman,
            ProficiencyXpLevel = 1,
            ProficiencyXp = 0,
            CharacterKnowledge = []
        });

        var knowledgeRepository = new InMemoryCharacterKnowledgeRepository();

        int processorCalls = 0;
        ICraftingProcessor craftingProcessor = new StubCraftingProcessor(() =>
        {
            processorCalls++;
            return Task.FromResult(new CraftingResult
            {
                Success = true,
                Message = "ok",
                ProductsCreated = [],
                ProgressionPointsAwarded = 250,
                ProficiencyXpAwarded = 0
            });
        });

        InMemoryIndustryRepository craftIndustryRepository = new InMemoryIndustryRepository();
        craftIndustryRepository.Add(industry);

        IProficiencyProgressionService proficiencyService = new StubProficiencyService();

        // Reuse the SetUp dispatcher, which already includes the award handler, so the
        // craft award flows through the real dispatch boundary.
        CraftItemHandler craftHandler = new(
            industryRepository: craftIndustryRepository,
            membershipRepository,
            knowledgeRepository,
            craftingProcessor,
            proficiencyService,
            commandDispatcher: _dispatcher,
            new RecipeTemplateExpander(
                new EmptyRecipeTemplateRepository(),
                new EmptyItemDefinitionRepository()));

        CommandResult result = await craftHandler.HandleAsync(new CraftItemCommand
        {
            CharacterId = TestCharacterId,
            IndustryTag = new IndustryTag("test_crafting"),
            RecipeId = new RecipeId("test_item"),
            InputQualities = []
        });

        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["knowledgePointsEarned"], Is.EqualTo(2));
        Assert.That((int)result.Data!["newTotalKnowledgePoints"], Is.EqualTo(2));
        Assert.That(processorCalls, Is.EqualTo(1), "Crafting processor must run exactly once per craft");
    }

    /// <summary>
    /// <see cref="IWorldConfigProvider"/> returning the progression defaults, overridable per test.
    /// </summary>
    private sealed class TestWorldConfigProvider : IWorldConfigProvider
    {
        private readonly Dictionary<string, object> _values = new();

        public bool GetBoolean(string key) => (bool)_values[key];
        public int? GetInt(string key) =>
            _values.TryGetValue(key, out object? value) && value is int i ? (int?)i : (int?)null;
        public float? GetFloat(string key) =>
            _values.TryGetValue(key, out object? value) && value is float f ? (float?)f : (float?)null;
        public string? GetString(string key) =>
            _values.TryGetValue(key, out object? value) && value is string s ? s : null;

        public void SetBoolean(string key, bool value) => _values[key] = value;
        public void SetInt(string key, int value) => _values[key] = value;
        public void SetFloat(string key, float value) => _values[key] = value;
        public void SetString(string key, string value) => _values[key] = value;
    }

    private sealed class StubCraftingProcessor : ICraftingProcessor
    {
        private readonly Func<Task<CraftingResult>> _factory;

        public StubCraftingProcessor(Func<Task<CraftingResult>> factory) => _factory = factory;

        public Task<CraftingResult> ProcessCraftingAsync(CharacterId characterId, Recipe recipe,
            int baseQuality, AggregatedCraftingModifiers modifiers, Dictionary<string, object> context) =>
            _factory();
    }

    private sealed class StubProficiencyService : IProficiencyProgressionService
    {
        public ProficiencyXpResult AwardProficiencyXp(IndustryMembership membership, int xp) =>
            new() { Success = true, NewLevel = membership.ProficiencyXpLevel };
        public int GetXpForNextLevel(int level) => 100;
        public bool CanGainXp(IndustryMembership membership) => true;
        public ProficiencyLevel GetTierForLevel(int level) => ProficiencyLevel.Novice;
    }
}
