using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.SharedKernel;

[TestFixture]
public class CharacterIdTests
{
    [Test]
    public void From_WithValidGuid_CreatesCharacterId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var characterId = CharacterId.From(guid);

        // Assert
        Assert.That(characterId.Value, Is.EqualTo(guid));
    }

    [Test]
    public void From_WithEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => CharacterId.From(Guid.Empty));
        Assert.That(ex!.ParamName, Is.EqualTo("id"));
        Assert.That(ex.Message, Does.Contain("CharacterId cannot be empty"));
    }

    [Test]
    public void New_CreatesUniqueCharacterId()
    {
        // Act
        var id1 = CharacterId.New();
        var id2 = CharacterId.New();

        // Assert
        Assert.That(id1.Value, Is.Not.EqualTo(Guid.Empty));
        Assert.That(id2.Value, Is.Not.EqualTo(Guid.Empty));
        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var id1 = CharacterId.From(guid);
        var id2 = CharacterId.From(guid);

        // Act & Assert
        Assert.That(id1, Is.EqualTo(id2));
        Assert.That(id1.GetHashCode(), Is.EqualTo(id2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var id1 = CharacterId.New();
        var id2 = CharacterId.New();

        // Act & Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void ImplicitConversionToGuid_ReturnsUnderlyingValue()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var characterId = CharacterId.From(guid);

        // Act
        Guid result = characterId;

        // Assert
        Assert.That(result, Is.EqualTo(guid));
    }

    [Test]
    public void ExplicitConversionFromGuid_CreatesCharacterId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var characterId = (CharacterId)guid;

        // Assert
        Assert.That(characterId.Value, Is.EqualTo(guid));
    }

    [Test]
    public void ExplicitConversionFromEmptyGuid_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (CharacterId)Guid.Empty);
    }

    [Test]
    public void ToString_ReturnsGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var characterId = CharacterId.From(guid);

        // Act
        var result = characterId.ToString();

        // Assert
        Assert.That(result, Is.EqualTo(guid.ToString()));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var id1 = CharacterId.New();
        var id2 = CharacterId.New();
        var dict = new Dictionary<CharacterId, string>
        {
            [id1] = "Player1",
            [id2] = "Player2"
        };

        // Act & Assert
        Assert.That(dict[id1], Is.EqualTo("Player1"));
        Assert.That(dict[id2], Is.EqualTo("Player2"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }
}
