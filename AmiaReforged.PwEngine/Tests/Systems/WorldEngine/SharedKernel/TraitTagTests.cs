using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.SharedKernel;

[TestFixture]
public class TraitTagTests
{
    [Test]
    public void Constructor_WithValidString_CreatesTraitTag()
    {
        // Arrange
        const string value = "brave";

        // Act
        var tag = new TraitTag(value);

        // Assert
        Assert.That(tag.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TraitTag(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("TraitTag cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TraitTag(""));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TraitTag("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void Constructor_WithStringExceeding50Characters_ThrowsArgumentException()
    {
        // Arrange
        var longString = new string('a', 51);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TraitTag(longString));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("cannot exceed 50 characters"));
    }

    [Test]
    public void Constructor_WithExactly50Characters_Succeeds()
    {
        // Arrange
        var string50 = new string('a', 50);

        // Act
        var tag = new TraitTag(string50);

        // Assert
        Assert.That(tag.Value, Is.EqualTo(string50));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        var tag1 = new TraitTag("brave");
        var tag2 = new TraitTag("brave");

        // Act & Assert
        Assert.That(tag1, Is.EqualTo(tag2));
        Assert.That(tag1.GetHashCode(), Is.EqualTo(tag2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var tag1 = new TraitTag("brave");
        var tag2 = new TraitTag("cowardly");

        // Act & Assert
        Assert.That(tag1, Is.Not.EqualTo(tag2));
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        var tag = new TraitTag("brave");

        // Act
        string result = tag;

        // Assert
        Assert.That(result, Is.EqualTo("brave"));
    }

    [Test]
    public void ExplicitConversionFromString_CreatesTraitTag()
    {
        // Arrange
        const string value = "brave";

        // Act
        var tag = (TraitTag)value;

        // Assert
        Assert.That(tag.Value, Is.EqualTo(value));
    }

    [Test]
    public void ExplicitConversionFromEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (TraitTag)"");
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var tag = new TraitTag("brave");

        // Act
        var result = tag.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("brave"));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var tag1 = new TraitTag("brave");
        var tag2 = new TraitTag("cowardly");
        var dict = new Dictionary<TraitTag, int>
        {
            [tag1] = 1,
            [tag2] = -1
        };

        // Act & Assert
        Assert.That(dict[tag1], Is.EqualTo(1));
        Assert.That(dict[tag2], Is.EqualTo(-1));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var tag1 = new TraitTag("brave");
        var tag2 = new TraitTag("brave");
        var tag3 = new TraitTag("cowardly");
        var set = new HashSet<TraitTag> { tag1, tag2, tag3 };

        // Act & Assert
        Assert.That(set.Count, Is.EqualTo(2)); // tag1 and tag2 are equal
        Assert.That(set.Contains(tag1), Is.True);
        Assert.That(set.Contains(tag3), Is.True);
    }
}
