using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.SharedKernel;

[TestFixture]
public class TraitTagTests
{
    [Test]
    public void Constructor_WithValidString_CreatesTraitTag()
    {
        // Arrange
        const string value = "brave";

        // Act
        TraitTag tag = new TraitTag(value);

        // Assert
        Assert.That(tag.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithNullString_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new TraitTag(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("TraitTag cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyString_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new TraitTag(""));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new TraitTag("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
    }

    [Test]
    public void Constructor_WithStringExceeding50Characters_ThrowsArgumentException()
    {
        // Arrange
        string longString = new string('a', 51);

        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new TraitTag(longString));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("cannot exceed 50 characters"));
    }

    [Test]
    public void Constructor_WithExactly50Characters_Succeeds()
    {
        // Arrange
        string string50 = new string('a', 50);

        // Act
        TraitTag tag = new TraitTag(string50);

        // Assert
        Assert.That(tag.Value, Is.EqualTo(string50));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        TraitTag tag1 = new TraitTag("brave");
        TraitTag tag2 = new TraitTag("brave");

        // Act & Assert
        Assert.That(tag1, Is.EqualTo(tag2));
        Assert.That(tag1.GetHashCode(), Is.EqualTo(tag2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        TraitTag tag1 = new TraitTag("brave");
        TraitTag tag2 = new TraitTag("cowardly");

        // Act & Assert
        Assert.That(tag1, Is.Not.EqualTo(tag2));
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        TraitTag tag = new TraitTag("brave");

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
        TraitTag tag = (TraitTag)value;

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
        TraitTag tag = new TraitTag("brave");

        // Act
        string result = tag.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("brave"));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        TraitTag tag1 = new TraitTag("brave");
        TraitTag tag2 = new TraitTag("cowardly");
        Dictionary<TraitTag, int> dict = new Dictionary<TraitTag, int>
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
        TraitTag tag1 = new TraitTag("brave");
        TraitTag tag2 = new TraitTag("brave");
        TraitTag tag3 = new TraitTag("cowardly");
        HashSet<TraitTag> set = new HashSet<TraitTag> { tag1, tag2, tag3 };

        // Act & Assert
        Assert.That(set.Count, Is.EqualTo(2)); // tag1 and tag2 are equal
        Assert.That(set.Contains(tag1), Is.True);
        Assert.That(set.Contains(tag3), Is.True);
    }
}
