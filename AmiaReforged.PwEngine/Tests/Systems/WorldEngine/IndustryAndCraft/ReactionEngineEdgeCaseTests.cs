using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class ReactionEngineEdgeCaseTests
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
        _reactions = new Mock<IReactionDefinitionRepository>(MockBehavior.Strict);
        _knowledge = new Mock<ICharacterKnowledgeRepository>(MockBehavior.Strict);
        _inventory = new Mock<IInventoryPort>(MockBehavior.Strict);
        _tooling = new Mock<IToolingPort>(MockBehavior.Strict);
        _rng = new DeterministicRandomPort(0.5);

        _actorId = Guid.NewGuid();
        _reactionId = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        _reactions.VerifyAll();
        _knowledge.VerifyAll();
        _inventory.VerifyAll();
        _tooling.VerifyAll();
    }

    [Test]
    public Task Execute_WithZeroOutputAfterMultiplier_ThrowsArgumentOutOfRangeException()
    {
        // Arrange - modifier that reduces output to zero
        KnowledgeModifier reductionModifier = new(KnowledgeKey.From("WASTEFUL"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("PRODUCT"), 0.0 } }
        };

        ReactionDefinition reaction = new(
            _reactionId,
            "Wasteful Craft",
            [new Quantity(ItemTag.From("INPUT"), 1)],
            [new Quantity(ItemTag.From("PRODUCT"), 5)],
            TimeSpan.FromMinutes(10),
            1.0,
            modifiers: [reductionModifier]);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { KnowledgeKey.From("WASTEFUL") });

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReactionEngine sut = CreateEngine();

        // Act & Assert
        // The ReactionEngine tries to create a Quantity with amount=0, which throws
        ArgumentOutOfRangeException? ex = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext()));

        Assert.That(ex?.ParamName, Is.EqualTo("amount"));

        // Verify inputs were still consumed before the exception
        _inventory.Verify(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()), Times.Once);
        return Task.CompletedTask;
    }

    [Test]
    public async Task Execute_WithFractionalOutputMultiplier_FlooredToInteger()
    {
        // Arrange - modifier that creates fractional output
        KnowledgeModifier fractionalModifier = new(KnowledgeKey.From("PARTIAL_EFFICIENCY"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("BOARD"), 0.7 } }
        };

        ReactionDefinition reaction = new(
            _reactionId,
            "Partial Craft",
            [new Quantity(ItemTag.From("LOG"), 1)],
            [new Quantity(ItemTag.From("BOARD"), 3)], // 3 * 0.7 = 2.1, floored to 2
            TimeSpan.FromMinutes(10),
            1.0,
            modifiers: [fractionalModifier]);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { KnowledgeKey.From("PARTIAL_EFFICIENCY") });

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _inventory
            .Setup(i => i.ProduceAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionResult result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext());

        // Assert
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Produced.Count, Is.EqualTo(1));
        Quantity producedBoard = result.Produced.First(q => q.Item == ItemTag.From("BOARD"));
        Assert.That(producedBoard.Amount, Is.EqualTo(2)); // 3 * 0.7 = 2.1, floored to 2
    }

    [Test]
    public async Task Execute_WithOutputMultipliers_AppliesCorrectAmounts()
    {
        // Arrange
        KnowledgeModifier knowledgeModifier = new(KnowledgeKey.From("EFFICIENCY"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("BOARD"), 1.5 } }
        };

        ReactionDefinition reaction = new(
            _reactionId,
            "Efficient Craft",
            [new Quantity(ItemTag.From("LOG"), 1)],
            [new Quantity(ItemTag.From("BOARD"), 4)], // Base output of 4
            TimeSpan.FromMinutes(10),
            1.0,
            modifiers: [knowledgeModifier]);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { KnowledgeKey.From("EFFICIENCY") });

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _inventory
            .Setup(i => i.ProduceAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionResult result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext());

        // Assert
        Assert.That(result.Succeeded, Is.True);
        Quantity producedBoard = result.Produced.First(q => q.Item == ItemTag.From("BOARD"));
        Assert.That(producedBoard.Amount, Is.EqualTo(6)); // 4 * 1.5 = 6
    }

    [Test]
    public async Task Execute_WithMultipleModifiersOnSameOutput_AppliesAllMultipliers()
    {
        // Arrange - multiple modifiers that affect the same output
        KnowledgeModifier knowledgeModifier = new(KnowledgeKey.From("EFFICIENCY"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("WIDGET"), 1.5 } }
        };

        ToolModifier toolModifier = new(ToolTag.From("PRECISION_TOOL"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("WIDGET"), 1.2 } }
        };

        ReactionDefinition reaction = new(
            _reactionId,
            "Precision Craft",
            [new Quantity(ItemTag.From("MATERIAL"), 1)],
            [new Quantity(ItemTag.From("WIDGET"), 10)], // Base: 10
            TimeSpan.FromMinutes(15),
            1.0,
            modifiers: [knowledgeModifier, toolModifier]);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { KnowledgeKey.From("EFFICIENCY") });

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance> { new(ToolTag.From("PRECISION_TOOL")) });

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _inventory
            .Setup(i => i.ProduceAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionResult result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext());

        // Assert
        Assert.That(result.Succeeded, Is.True);
        Quantity producedWidget = result.Produced.First(q => q.Item == ItemTag.From("WIDGET"));
        // 10 * 1.5 (knowledge) * 1.2 (tool) = 18
        Assert.That(producedWidget.Amount, Is.EqualTo(18));
    }

    [Test]
    public Task Execute_WithVerySmallMultiplier_ResultsInZeroAndThrows()
    {
        // Arrange - a very small multiplier that results in a fractional amount < 1
        KnowledgeModifier tinyModifier = new(KnowledgeKey.From("TINY_EFFECT"))
        {
            OutputMultipliers = new Dictionary<ItemTag, double> { { ItemTag.From("ITEM"), 0.1 } }
        };

        ReactionDefinition reaction = new(
            _reactionId,
            "Tiny Output Craft",
            [new Quantity(ItemTag.From("INPUT"), 1)],
            [new Quantity(ItemTag.From("ITEM"), 2)], // 2 * 0.1 = 0.2, floored to 0
            TimeSpan.FromMinutes(5),
            1.0,
            modifiers: [tinyModifier]);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { KnowledgeKey.From("TINY_EFFECT") });

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ReactionEngine sut = CreateEngine();

        // Act & Assert
        ArgumentOutOfRangeException? ex = Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext()));

        Assert.That(ex?.ParamName, Is.EqualTo("amount"));
        return Task.CompletedTask;
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

    private sealed class DeterministicRandomPort(double value) : IRandomPort
    {
        private readonly double _value = Math.Clamp(value, 0, 0.999999999999);
        public double NextUnit() => _value;
    }
}
