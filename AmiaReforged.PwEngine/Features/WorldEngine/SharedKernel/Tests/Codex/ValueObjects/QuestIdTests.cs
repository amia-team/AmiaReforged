using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.ValueObjects;

[TestFixture]
public class QuestIdTests
{
    [Test]
    public void Constructor_WithValidValue_CreatesQuestId()
    {
        // Arrange
        const string value = "quest_dragons_lair";

        // Act
        QuestId questId = new QuestId(value);

        // Assert
        Assert.That(questId.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithNullValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new QuestId(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("QuestId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new QuestId(string.Empty));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("QuestId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new QuestId("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("QuestId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithValueExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        string longValue = new string('a', 101);

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new QuestId(longValue));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("QuestId cannot exceed 100 characters"));
    }

    [Test]
    public void Constructor_WithExactlyMaxLength_Succeeds()
    {
        // Arrange
        string maxLengthValue = new string('a', 100);

        // Act
        QuestId questId = new QuestId(maxLengthValue);

        // Assert
        Assert.That(questId.Value, Is.EqualTo(maxLengthValue));
        Assert.That(questId.Value.Length, Is.EqualTo(100));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        const string value = "quest_ancient_ruins";
        QuestId id1 = new QuestId(value);
        QuestId id2 = new QuestId(value);

        // Act & Assert
        Assert.That(id1, Is.EqualTo(id2));
        Assert.That(id1.GetHashCode(), Is.EqualTo(id2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        QuestId id1 = new QuestId("quest_one");
        QuestId id2 = new QuestId("quest_two");

        // Act & Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        const string value = "quest_forbidden_forest";
        QuestId questId = new QuestId(value);

        // Act
        string result = questId;

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromString_CreatesValueObject()
    {
        // Arrange
        const string value = "quest_mystic_tower";

        // Act
        QuestId questId = (QuestId)value;

        // Assert
        Assert.That(questId.Value, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (QuestId)string.Empty);
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        const string value = "quest_treasure_hunt";
        QuestId questId = new QuestId(value);

        // Act
        string result = questId.ToString();

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        QuestId id1 = new QuestId("quest_one");
        QuestId id2 = new QuestId("quest_two");
        Dictionary<QuestId, string> dict = new Dictionary<QuestId, string>
        {
            [id1] = "Dragon's Lair",
            [id2] = "Ancient Ruins"
        };

        // Act & Assert
        Assert.That(dict[id1], Is.EqualTo("Dragon's Lair"));
        Assert.That(dict[id2], Is.EqualTo("Ancient Ruins"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        QuestId id1 = new QuestId("quest_one");
        QuestId id2 = new QuestId("quest_two");
        QuestId id3 = new QuestId("quest_one"); // Duplicate

        HashSet<QuestId> hashSet = new HashSet<QuestId> { id1, id2, id3 };

        // Act & Assert
        Assert.That(hashSet.Count, Is.EqualTo(2)); // id3 is duplicate of id1
        Assert.That(hashSet.Contains(id1), Is.True);
        Assert.That(hashSet.Contains(id2), Is.True);
    }
}
