using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class ReactionEngineActorBuildingTests
{
    private Mock<IReactionDefinitionRepository> _reactions = null!;
    private Mock<ICharacterKnowledgeRepository> _knowledge = null!;
    private Mock<IInventoryPort> _inventory = null!;
    private Mock<IToolingPort> _tooling = null!;
    private IRandomPort _rng = null!;

    private Guid _actorId;

    [SetUp]
    public void SetUp()
    {
        _reactions = new Mock<IReactionDefinitionRepository>(MockBehavior.Loose);
        _knowledge = new Mock<ICharacterKnowledgeRepository>(MockBehavior.Strict);
        _inventory = new Mock<IInventoryPort>(MockBehavior.Loose);
        _tooling = new Mock<IToolingPort>(MockBehavior.Strict);
        _rng = new DeterministicRandomPort(0.5);

        _actorId = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        _knowledge.VerifyAll();
        _tooling.VerifyAll();
    }

    [Test]
    public async Task BuildActorAsync_ReturnsActorWithCorrectKnowledgeAndTools()
    {
        // Arrange
        HashSet<KnowledgeKey> expectedKnowledge =
        [
            KnowledgeKey.From("WOODWORKING"),
            KnowledgeKey.From("CRAFTING")
        ];
        List<ToolInstance> expectedTools =
        [
            new(ToolTag.From("SAW"), 75),
            new(ToolTag.From("HAMMER"))
        ];

        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedKnowledge);

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTools);

        ReactionEngine sut = CreateEngine();

        // Act
        IReactionActor actor = await sut.BuildActorAsync(_actorId);

        // Assert
        Assert.That(actor.ActorId, Is.EqualTo(_actorId));
        Assert.That(actor.Knowledge.Count, Is.EqualTo(2));
        Assert.That(actor.Knowledge.Contains(KnowledgeKey.From("WOODWORKING")), Is.True);
        Assert.That(actor.Knowledge.Contains(KnowledgeKey.From("CRAFTING")), Is.True);
        Assert.That(actor.Tools.Length, Is.EqualTo(2));
        Assert.That(actor.Tools.Any(t => t.Tag == ToolTag.From("SAW") && t.Quality == 75), Is.True);
        Assert.That(actor.Tools.Any(t => t.Tag == ToolTag.From("HAMMER") && t.Quality == 50), Is.True);
    }

    [Test]
    public async Task BuildActorAsync_WithEmptyKnowledgeAndTools_ReturnsEmptyActor()
    {
        // Arrange
        _knowledge
            .Setup(k => k.GetSetAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<KnowledgeKey>());

        _tooling
            .Setup(t => t.GetToolsAsync(_actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ToolInstance>());

        ReactionEngine sut = CreateEngine();

        // Act
        IReactionActor actor = await sut.BuildActorAsync(_actorId);

        // Assert
        Assert.That(actor.ActorId, Is.EqualTo(_actorId));
        Assert.That(actor.Knowledge.Count, Is.EqualTo(0));
        Assert.That(actor.Tools.Length, Is.EqualTo(0));
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
