using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.Codex.ValueObjects;

[TestFixture]
public class LoreIdTests
{
    [Test]
    public void Constructor_WithValidValue_CreatesLoreId()
    {
        // Arrange
        const string value = "lore_ancient_history";

        // Act
        LoreId loreId = new LoreId(value);

        // Assert
        Assert.That(loreId.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithNullValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new LoreId(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new LoreId(string.Empty));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new LoreId("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithValueExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        string longValue = new string('a', 101);

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new LoreId(longValue));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("LoreId cannot exceed 100 characters"));
    }

    [Test]
    public void Constructor_WithExactlyMaxLength_Succeeds()
    {
        // Arrange
        string maxLengthValue = new string('a', 100);

        // Act
        LoreId loreId = new LoreId(maxLengthValue);

        // Assert
        Assert.That(loreId.Value, Is.EqualTo(maxLengthValue));
        Assert.That(loreId.Value.Length, Is.EqualTo(100));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        const string value = "lore_elven_legends";
        LoreId id1 = new LoreId(value);
        LoreId id2 = new LoreId(value);

        // Act & Assert
        Assert.That(id1, Is.EqualTo(id2));
        Assert.That(id1.GetHashCode(), Is.EqualTo(id2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        LoreId id1 = new LoreId("lore_one");
        LoreId id2 = new LoreId("lore_two");

        // Act & Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        const string value = "lore_dragon_tales";
        LoreId loreId = new LoreId(value);

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
        LoreId loreId = (LoreId)value;

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
        LoreId loreId = new LoreId(value);

        // Act
        string result = loreId.ToString();

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        LoreId id1 = new LoreId("lore_one");
        LoreId id2 = new LoreId("lore_two");
        Dictionary<LoreId, string> dict = new Dictionary<LoreId, string>
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
        LoreId id1 = new LoreId("lore_one");
        LoreId id2 = new LoreId("lore_two");
        LoreId id3 = new LoreId("lore_one"); // Duplicate

        HashSet<LoreId> hashSet = new HashSet<LoreId> { id1, id2, id3 };

        // Act & Assert
        Assert.That(hashSet.Count, Is.EqualTo(2)); // id3 is duplicate of id1
        Assert.That(hashSet.Contains(id1), Is.True);
        Assert.That(hashSet.Contains(id2), Is.True);
    }
}
