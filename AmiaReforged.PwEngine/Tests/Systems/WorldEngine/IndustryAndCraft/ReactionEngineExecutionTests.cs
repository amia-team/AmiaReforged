using System.Collections.Immutable;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class ReactionEngineExecutionTests
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
        _rng = new DeterministicRandomPort(0.123);

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
    public async Task Execute_WithFailedPreconditions_ReturnsFailureWithReasons()
    {
        // Arrange
        var reaction = new ReactionDefinition(
            _reactionId,
            "Test Reaction",
            ImmutableArray.Create(new Quantity(ItemTag.From("WOOD"), 1)),
            ImmutableArray.Create(new Quantity(ItemTag.From("PLANK"), 1)),
            TimeSpan.FromMinutes(10),
            1.0,
            [new RequiresKnowledge(KnowledgeKey.From("WOODWORKING")), new RequiresTool(ToolTag.From("SAW"))]);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>());

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        var sut = CreateEngine();

        // Act
        var result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext());

        // Assert
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(result.Produced, Is.Empty);
        Assert.That(result.Notes.Count, Is.EqualTo(2));
        Assert.That(result.Notes, Does.Contain("Requires knowledge 'woodworking'."));
        Assert.That(result.Notes, Does.Contain("Requires tool 'saw'."));
    }

    [Test]
    public async Task Execute_MissingInputItems_ReturnsFailureWithNote()
    {
        // Arrange
        var reaction = new ReactionDefinition(
            _reactionId,
            "Test Reaction",
            ImmutableArray.Create(new Quantity(ItemTag.From("WOOD"), 5)),
            ImmutableArray.Create(new Quantity(ItemTag.From("PLANK"), 2)),
            TimeSpan.FromMinutes(10),
            1.0);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>());

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateEngine();

        // Act
        var result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext());

        // Assert
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.Zero));
        Assert.That(result.Produced, Is.Empty);
        Assert.That(result.Notes, Does.Contain("Missing required inputs."));
    }

    [Test]
    public async Task Execute_WhenFeasible_ProducesOutputs_AndReportsSuccess()
    {
        // Arrange
        ReactionDefinition def = new ReactionDefinition(
            id: _reactionId,
            name: "Craft Board",
            inputs: ImmutableArray.Create(new Quantity(ItemTag.From("LOG"), 1)),
            outputs: ImmutableArray.Create(new Quantity(ItemTag.From("BOARD"), 2)),
            baseDuration: TimeSpan.FromMinutes(30),
            baseSuccessChance: 1.0,
            preconditions: [new RequiresKnowledge(KnowledgeKey.From("WOODWORKING")), new RequiresTool(ToolTag.From("SAW"))],
            modifiers: []);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(def);

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.Is<IReadOnlyList<Quantity>>(req =>
                    req.Count == 1 && req[0].Item == ItemTag.From("LOG") && req[0].Amount == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _inventory
            .Setup(i => i.ProduceAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey> { KnowledgeKey.From("WOODWORKING") });

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance> { new(ToolTag.From("SAW"), quality: 60) });

        ReactionEngine sut = CreateEngine();

        // Act
        ReactionResult result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext(), CancellationToken.None);

        // Assert
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Duration, Is.GreaterThan(TimeSpan.Zero));
        Assert.That(result.Produced, Is.Not.Null);
        Assert.That(result.Produced.Count, Is.GreaterThanOrEqualTo(1));

        Quantity? producedBoard = result.Produced.FirstOrDefault(q => q.Item == ItemTag.From("BOARD"));
        Assert.That(producedBoard, Is.Not.Null);
        Assert.That(producedBoard!.Amount, Is.GreaterThanOrEqualTo(2));

        // Verify inventory operations
        _inventory.Verify(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()), Times.Once);
        _inventory.Verify(i => i.ProduceAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Execute_WithLowSuccessChance_FailsAndConsumesInputsButProducesNothing()
    {
        // Arrange - use random port that always fails
        var failingRng = new DeterministicRandomPort(0.9);

        var reaction = new ReactionDefinition(
            _reactionId,
            "Risky Craft",
            ImmutableArray.Create(new Quantity(ItemTag.From("RARE_MATERIAL"), 1)),
            ImmutableArray.Create(new Quantity(ItemTag.From("MASTERWORK"), 1)),
            TimeSpan.FromMinutes(60),
            0.1); // Very low success chance

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>());

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new ReactionEngine(_reactions.Object, _knowledge.Object, _tooling.Object, _inventory.Object, failingRng);

        // Act
        var result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext());

        // Assert
        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Duration, Is.EqualTo(TimeSpan.FromMinutes(60))); // Still takes time even when failing
        Assert.That(result.Produced, Is.Empty);

        // Verify inputs were consumed even on failure
        _inventory.Verify(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()), Times.Once);
        _inventory.Verify(i => i.ProduceAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Execute_WithMultipleInputsAndOutputs_HandlesCorrectly()
    {
        // Arrange
        var reaction = new ReactionDefinition(
            _reactionId,
            "Complex Craft",
            ImmutableArray.Create(
                new Quantity(ItemTag.From("WOOD"), 2),
                new Quantity(ItemTag.From("METAL"), 1)),
            ImmutableArray.Create(
                new Quantity(ItemTag.From("HANDLE"), 1),
                new Quantity(ItemTag.From("BLADE"), 1)),
            TimeSpan.FromMinutes(45),
            0.9);

        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reaction);

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>());

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        _inventory
            .Setup(i => i.HasItemsAsync(_actorId, It.Is<IReadOnlyList<Quantity>>(req =>
                req.Count == 2 &&
                req.Any(q => q.Item == ItemTag.From("WOOD") && q.Amount == 2) &&
                req.Any(q => q.Item == ItemTag.From("METAL") && q.Amount == 1)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _inventory
            .Setup(i => i.ConsumeAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _inventory
            .Setup(i => i.ProduceAsync(_actorId, It.IsAny<IReadOnlyList<Quantity>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateEngine();

        // Act
        var result = await sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext());

        // Assert
        Assert.That(result.Succeeded, Is.True);
        Assert.That(result.Produced.Count, Is.EqualTo(2));
        Assert.That(result.Produced.Any(q => q.Item == ItemTag.From("HANDLE") && q.Amount == 1), Is.True);
        Assert.That(result.Produced.Any(q => q.Item == ItemTag.From("BLADE") && q.Amount == 1), Is.True);
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
