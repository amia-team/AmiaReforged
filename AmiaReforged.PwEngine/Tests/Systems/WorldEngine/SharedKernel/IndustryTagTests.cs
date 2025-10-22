using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.SharedKernel;

[TestFixture]
public class IndustryTagTests
{
    [Test]
    public void Constructor_WithValidString_CreatesIndustryTag()
    {
        // Arrange
        const string value = "blacksmithing";

        // Act
        var tag = new IndustryTag(value);

        // Assert
        Assert.That(tag.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new IndustryTag(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("IndustryTag cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new IndustryTag(""));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new IndustryTag("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void Constructor_WithStringExceeding50Characters_ThrowsArgumentException()
    {
        // Arrange
        var longString = new string('a', 51);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new IndustryTag(longString));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("cannot exceed 50 characters"));
    }

    [Test]
    public void Constructor_WithExactly50Characters_Succeeds()
    {
        // Arrange
        var string50 = new string('a', 50);

        // Act
        var tag = new IndustryTag(string50);

        // Assert
        Assert.That(tag.Value, Is.EqualTo(string50));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        var tag1 = new IndustryTag("blacksmithing");
        var tag2 = new IndustryTag("blacksmithing");

        // Act & Assert
        Assert.That(tag1, Is.EqualTo(tag2));
        Assert.That(tag1.GetHashCode(), Is.EqualTo(tag2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var tag1 = new IndustryTag("blacksmithing");
        var tag2 = new IndustryTag("alchemy");

        // Act & Assert
        Assert.That(tag1, Is.Not.EqualTo(tag2));
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        var tag = new IndustryTag("blacksmithing");

        // Act
        string result = tag;

        // Assert
        Assert.That(result, Is.EqualTo("blacksmithing"));
    }

    [Test]
    public void ExplicitConversionFromString_CreatesIndustryTag()
    {
        // Arrange
        const string value = "blacksmithing";

        // Act
        var tag = (IndustryTag)value;

        // Assert
        Assert.That(tag.Value, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (IndustryTag)"");
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var tag = new IndustryTag("blacksmithing");

        // Act
        var result = tag.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("blacksmithing"));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var tag1 = new IndustryTag("blacksmithing");
        var tag2 = new IndustryTag("alchemy");
        var dict = new Dictionary<IndustryTag, int>
        {
            [tag1] = 10,
            [tag2] = 5
        };

        // Act & Assert
        Assert.That(dict[tag1], Is.EqualTo(10));
        Assert.That(dict[tag2], Is.EqualTo(5));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var tag1 = new IndustryTag("blacksmithing");
        var tag2 = new IndustryTag("blacksmithing");
        var tag3 = new IndustryTag("alchemy");
        var set = new HashSet<IndustryTag> { tag1, tag2, tag3 };

        // Act & Assert
        Assert.That(set.Count, Is.EqualTo(2)); // tag1 and tag2 are equal
        Assert.That(set.Contains(tag1), Is.True);
        Assert.That(set.Contains(tag3), Is.True);
    }
}
