using System.Collections.Immutable;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class ReactionEngineTests
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
    public async Task Execute_ReactionNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        _reactions
            .Setup(r => r.FindByIdAsync(_reactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReactionDefinition?)null);

        ReactionEngine sut = CreateEngine();

        // Act & Assert
        InvalidOperationException? ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(_reactionId, _actorId, new ReactionContext()));

        Assert.That(ex.Message, Is.EqualTo("Reaction not found."));
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
