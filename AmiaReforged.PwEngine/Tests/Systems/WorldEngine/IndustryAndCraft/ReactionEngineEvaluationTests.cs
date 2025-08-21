using System.Collections.Immutable;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class ReactionEngineEvaluationTests
{
    private Mock<IReactionDefinitionRepository> _reactions = null!;
    private Mock<ICharacterKnowledgeRepository> _knowledge = null!;
    private Mock<IInventoryPort> _inventory = null!;
    private Mock<IToolingPort> _tooling = null!;
    private IRandomPort _rng = null!;

    private Guid _actorId;
    private Guid _reactionId;

    [SetUp]
    public void SetUp()
    {
        _reactions = new Mock<IReactionDefinitionRepository>(MockBehavior.Loose);
        _knowledge = new Mock<ICharacterKnowledgeRepository>(MockBehavior.Loose);
        _inventory = new Mock<IInventoryPort>(MockBehavior.Loose);
        _tooling = new Mock<IToolingPort>(MockBehavior.Loose);
        _rng = new DeterministicRandomPort(0.5);

        _actorId = Guid.NewGuid();
        _reactionId = Guid.NewGuid();
    }

    [Test]
    public void Evaluate_WithFailingPreconditions_ReturnsFeasibilityWithCanExecuteFalse()
    {
        // Arrange
        ReactionDefinition reaction = new ReactionDefinition(
            _reactionId,
            "Test Reaction",
            ImmutableArray.Create(new Quantity(ItemTag.From("WOOD"), 1)),
            ImmutableArray.Create(new Quantity(ItemTag.From("PLANK"), 1)),
            TimeSpan.FromMinutes(10),
            0.8,
            new[] { new RequiresKnowledge(KnowledgeKey.From("WOODWORKING")) });

        IReactionActor actor = CreateMockActor(knowledge: new HashSet<KnowledgeKey>(), tools: new List<ToolInstance>());
        ReactionContext context = new ReactionContext();

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionFeasibility feasibility = sut.Evaluate(reaction, context, actor);

        // Assert
        Assert.That(feasibility.CanExecute, Is.False);
        Assert.That(feasibility.PreconditionResults.Count, Is.EqualTo(1));
        Assert.That(feasibility.PreconditionResults[0].Satisfied, Is.False);
        Assert.That(feasibility.PreconditionResults[0].ReasonCode, Is.EqualTo("missing_knowledge"));
    }

    [Test]
    public void Evaluate_WithSuccessfulPreconditions_ReturnsFeasibilityWithCanExecuteTrue()
    {
        // Arrange
        ReactionDefinition reaction = new ReactionDefinition(
            _reactionId,
            "Test Reaction",
            ImmutableArray.Create(new Quantity(ItemTag.From("WOOD"), 1)),
            ImmutableArray.Create(new Quantity(ItemTag.From("PLANK"), 1)),
            TimeSpan.FromMinutes(10),
            0.8,
            new[] { new RequiresKnowledge(KnowledgeKey.From("WOODWORKING")) });

        IReactionActor actor = CreateMockActor(
            knowledge: new HashSet<KnowledgeKey> { KnowledgeKey.From("WOODWORKING") },
            tools: new List<ToolInstance>());
        ReactionContext context = new ReactionContext();

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionFeasibility feasibility = sut.Evaluate(reaction, context, actor);

        // Assert
        Assert.That(feasibility.CanExecute, Is.True);
        Assert.That(feasibility.PreconditionResults.Count, Is.EqualTo(1));
        Assert.That(feasibility.PreconditionResults[0].Satisfied, Is.True);
        Assert.That(feasibility.Duration, Is.EqualTo(TimeSpan.FromMinutes(10)));
        Assert.That(feasibility.SuccessChance, Is.EqualTo(0.8));
    }

    [Test]
    public void Evaluate_WithKnowledgeModifier_AppliesModifications()
    {
        // Arrange
        KnowledgeModifier knowledgeModifier = new KnowledgeModifier(KnowledgeKey.From("ADVANCED_WOODWORKING"))
        {
            SuccessChanceDelta = 0.1,
            DurationMultiplier = 0.8,
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("PLANK"), 1.5 } }
        };

        ReactionDefinition reaction = new ReactionDefinition(
            _reactionId,
            "Test Reaction",
            ImmutableArray.Create(new Quantity(ItemTag.From("WOOD"), 1)),
            ImmutableArray.Create(new Quantity(ItemTag.From("PLANK"), 2)),
            TimeSpan.FromMinutes(10),
            0.5,
            modifiers: new[] { knowledgeModifier });

        IReactionActor actor = CreateMockActor(
            knowledge: new HashSet<KnowledgeKey> { KnowledgeKey.From("ADVANCED_WOODWORKING") },
            tools: new List<ToolInstance>());
        ReactionContext context = new ReactionContext();

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionFeasibility feasibility = sut.Evaluate(reaction, context, actor);

        // Assert
        Assert.That(feasibility.SuccessChance, Is.EqualTo(0.6)); // 0.5 + 0.1
        Assert.That(feasibility.Duration, Is.EqualTo(TimeSpan.FromMinutes(8))); // 10 * 0.8
        Assert.That(feasibility.OutputMultipliers[ItemTag.From("PLANK")], Is.EqualTo(1.5));
    }

    [Test]
    public void Evaluate_WithToolModifier_AppliesModifications()
    {
        // Arrange
        ToolModifier toolModifier = new ToolModifier(ToolTag.From("QUALITY_SAW"))
        {
            SuccessChanceDelta = 0.15,
            DurationMultiplier = 0.7,
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("PLANK"), 1.2 } }
        };

        ReactionDefinition reaction = new ReactionDefinition(
            _reactionId,
            "Test Reaction",
            ImmutableArray.Create(new Quantity(ItemTag.From("WOOD"), 1)),
            ImmutableArray.Create(new Quantity(ItemTag.From("PLANK"), 2)),
            TimeSpan.FromMinutes(10),
            0.5,
            modifiers: new[] { toolModifier });

        IReactionActor actor = CreateMockActor(
            knowledge: new HashSet<KnowledgeKey>(),
            tools: new List<ToolInstance> { new(ToolTag.From("QUALITY_SAW"), 80) });
        ReactionContext context = new ReactionContext();

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionFeasibility feasibility = sut.Evaluate(reaction, context, actor);

        // Assert
        // With quality 80, quality factor should be 1.0 + (80-50)/100 = 1.3
        Assert.That(feasibility.SuccessChance, Is.EqualTo(Math.Clamp(0.5 + 0.15 * 1.3, 0, 1)));
        Assert.That(feasibility.OutputMultipliers[ItemTag.From("PLANK")], Is.EqualTo(1.2));
    }

    [Test]
    public void Evaluate_WithExtremeModifiers_ClampsValuesCorrectly()
    {
        // Arrange
        KnowledgeModifier extremeModifier = new KnowledgeModifier(KnowledgeKey.From("EXTREME"))
        {
            SuccessChanceDelta = 2.0, // Would push success chance above 1.0
            DurationMultiplier = 0.0 // Would make duration zero
        };

        ReactionDefinition reaction = new ReactionDefinition(
            _reactionId,
            "Extreme Reaction",
            ImmutableArray.Create(new Quantity(ItemTag.From("INPUT"), 1)),
            ImmutableArray.Create(new Quantity(ItemTag.From("OUTPUT"), 1)),
            TimeSpan.FromMinutes(60),
            0.5,
            modifiers: new[] { extremeModifier });

        IReactionActor actor = CreateMockActor(
            knowledge: new HashSet<KnowledgeKey> { KnowledgeKey.From("EXTREME") },
            tools: new List<ToolInstance>());
        ReactionContext context = new ReactionContext();

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionFeasibility feasibility = sut.Evaluate(reaction, context, actor);

        // Assert
        Assert.That(feasibility.SuccessChance, Is.EqualTo(1.0)); // Should be clamped to 1.0
        Assert.That(feasibility.Duration, Is.EqualTo(TimeSpan.Zero)); // Duration can go to zero
    }

    private ReactionEngine CreateEngine()
    {
        return new ReactionEngine(
            _reactions.Object,
            _knowledge.Object,
            _tooling.Object,
            _inventory.Object,
            _rng
        );
    }

    private IReactionActor CreateMockActor(HashSet<KnowledgeKey> knowledge, List<ToolInstance> tools)
    {
        Mock<IReactionActor> mock = new Mock<IReactionActor>();
        mock.Setup(a => a.ActorId).Returns(_actorId);
        mock.Setup(a => a.Knowledge).Returns(knowledge.ToImmutableHashSet());
        mock.Setup(a => a.Tools).Returns(tools.ToImmutableArray());
        return mock.Object;
    }

    private sealed class DeterministicRandomPort(double value) : IRandomPort
    {
        private readonly double _value = Math.Clamp(value, 0, 0.999999999999);
        public double NextUnit() => _value;
    }
}
