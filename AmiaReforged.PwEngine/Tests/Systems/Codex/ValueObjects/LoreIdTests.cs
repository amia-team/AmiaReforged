using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.ValueObjects;

[TestFixture]
public class LoreIdTests
{
    [Test]
    public void Constructor_WithValidValue_CreatesLoreId()
    {
        // Arrange
        const string value = "lore_ancient_history";

        // Act
        var loreId = new LoreId(value);

        // Assert
        Assert.That(loreId.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithNullValue_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new LoreId(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new LoreId(string.Empty));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new LoreId("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithValueExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longValue = new string('a', 101);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new LoreId(longValue));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot exceed 100 characters"));
    }

    [Test]
    public void Constructor_WithExactlyMaxLength_Succeeds()
    {
        // Arrange
        var maxLengthValue = new string('a', 100);

        // Act
        var loreId = new LoreId(maxLengthValue);

        // Assert
        Assert.That(loreId.Value, Is.EqualTo(maxLengthValue));
        Assert.That(loreId.Value.Length, Is.EqualTo(100));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        const string value = "lore_elven_legends";
        var id1 = new LoreId(value);
        var id2 = new LoreId(value);

        // Act & Assert
        Assert.That(id1, Is.EqualTo(id2));
        Assert.That(id1.GetHashCode(), Is.EqualTo(id2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var id1 = new LoreId("lore_one");
        var id2 = new LoreId("lore_two");

        // Act & Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        const string value = "lore_dragon_tales";
        var loreId = new LoreId(value);

        // Act
        string result = loreId;

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromString_CreatesValueObject()
    {
        // Arrange
        const string value = "lore_dwarven_forge";

        // Act
        var loreId = (LoreId)value;

        // Assert
        Assert.That(loreId.Value, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (LoreId)string.Empty);
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        const string value = "lore_magic_theory";
        var loreId = new LoreId(value);

        // Act
        var result = loreId.ToString();

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var id1 = new LoreId("lore_one");
        var id2 = new LoreId("lore_two");
        var dict = new Dictionary<LoreId, string>
        {
            [id1] = "Ancient History",
            [id2] = "Elven Legends"
        };

        // Act & Assert
        Assert.That(dict[id1], Is.EqualTo("Ancient History"));
        Assert.That(dict[id2], Is.EqualTo("Elven Legends"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var id1 = new LoreId("lore_one");
        var id2 = new LoreId("lore_two");
        var id3 = new LoreId("lore_one"); // Duplicate

        var hashSet = new HashSet<LoreId> { id1, id2, id3 };

        // Act & Assert
        Assert.That(hashSet.Count, Is.EqualTo(2)); // id3 is duplicate of id1
        Assert.That(hashSet.Contains(id1), Is.True);
        Assert.That(hashSet.Contains(id2), Is.True);
    }
}
