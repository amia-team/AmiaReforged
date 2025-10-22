using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.ValueObjects;

[TestFixture]
public class FactionIdTests
{
    [Test]
    public void Constructor_WithValidValue_CreatesFactionId()
    {
        // Arrange
        const string value = "harpers";

        // Act
        var factionId = new FactionId(value);

        // Assert
        Assert.That(factionId.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithNullValue_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FactionId(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("FactionId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FactionId(string.Empty));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("FactionId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FactionId("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("FactionId cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithValueExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longValue = new string('a', 51);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new FactionId(longValue));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("FactionId cannot exceed 50 characters"));
    }

    [Test]
    public void Constructor_WithExactlyMaxLength_Succeeds()
    {
        // Arrange
        var maxLengthValue = new string('a', 50);

        // Act
        var factionId = new FactionId(maxLengthValue);

        // Assert
        Assert.That(factionId.Value, Is.EqualTo(maxLengthValue));
        Assert.That(factionId.Value.Length, Is.EqualTo(50));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        const string value = "zhentarim";
        var id1 = new FactionId(value);
        var id2 = new FactionId(value);

        // Act & Assert
        Assert.That(id1, Is.EqualTo(id2));
        Assert.That(id1.GetHashCode(), Is.EqualTo(id2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var id1 = new FactionId("harpers");
        var id2 = new FactionId("zhentarim");

        // Act & Assert
        Assert.That(id1, Is.Not.EqualTo(id2));
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        const string value = "lord_alliance";
        var factionId = new FactionId(value);

        // Act
        string result = factionId;

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromString_CreatesValueObject()
    {
        // Arrange
        const string value = "red_wizards";

        // Act
        var factionId = (FactionId)value;

        // Assert
        Assert.That(factionId.Value, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (FactionId)string.Empty);
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        const string value = "emerald_enclave";
        var factionId = new FactionId(value);

        // Act
        var result = factionId.ToString();

        // Assert
        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var id1 = new FactionId("harpers");
        var id2 = new FactionId("zhentarim");
        var dict = new Dictionary<FactionId, string>
        {
            [id1] = "The Harpers",
            [id2] = "The Zhentarim"
        };

        // Act & Assert
        Assert.That(dict[id1], Is.EqualTo("The Harpers"));
        Assert.That(dict[id2], Is.EqualTo("The Zhentarim"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var id1 = new FactionId("harpers");
        var id2 = new FactionId("zhentarim");
        var id3 = new FactionId("harpers"); // Duplicate

        var hashSet = new HashSet<FactionId> { id1, id2, id3 };

        // Act & Assert
        Assert.That(hashSet.Count, Is.EqualTo(2)); // id3 is duplicate of id1
        Assert.That(hashSet.Contains(id1), Is.True);
        Assert.That(hashSet.Contains(id2), Is.True);
    }
}
