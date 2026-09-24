using System;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Tests;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries.Tests;

/// <summary>
/// Focused tests for <see cref="GetCharacterStatsQueryHandler"/>. Uses the
/// in-memory repository double; requires no live NWN objects or database.
/// </summary>
[TestFixture]
public class GetCharacterStatsQueryHandlerTests
{
    private static readonly Guid TestCharacterId = Guid.NewGuid();

    private InMemoryCharacterStatRepository _repository = null!;
    private GetCharacterStatsQueryHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryCharacterStatRepository();
        _handler = new GetCharacterStatsQueryHandler(_repository);
    }

    private GetCharacterStatsQuery Query() => new(CharacterId.From(TestCharacterId));

    [Test]
    public async Task HandleAsync_KnownCharacter_ReturnsPlayTimeProjection()
    {
        // Arrange
        _repository.Seed(new CharacterStatistics { CharacterId = TestCharacterId, PlayTime = 42 });

        // Act
        CharacterStats? result = await _handler.HandleAsync(Query());

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PlayTime, Is.EqualTo(42));
    }

    [Test]
    public async Task HandleAsync_MissingCharacter_ReturnsNull()
    {
        // Act
        CharacterStats? result = await _handler.HandleAsync(new GetCharacterStatsQuery(CharacterId.New()));

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task HandleAsync_DoesNotProjectUnsupportedFields()
    {
        // Arrange: rank-ups, industries, deaths, and knowledge points exist, but
        // none map to a truthful statistics field.
        _repository.Seed(new CharacterStatistics
        {
            CharacterId = TestCharacterId,
            PlayTime = 7,
            TimesRankedUp = 5,
            IndustriesJoined = 3,
            TimesDied = 9,
            KnowledgePoints = 100
        });

        // Act
        CharacterStats? result = await _handler.HandleAsync(Query());

        // Assert: only play time survives the projection.
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PlayTime, Is.EqualTo(7));
    }
}
