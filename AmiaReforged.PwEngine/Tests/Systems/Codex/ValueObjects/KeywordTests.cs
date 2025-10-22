using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.ValueObjects;

[TestFixture]
public class KeywordTests
{
    [Test]
    public void Constructor_WithValidValue_CreatesKeyword()
    {
        // Arrange
        const string value = "dragon";

        // Act
        var keyword = new Keyword(value);

        // Assert
        Assert.That(keyword.Value, Is.EqualTo("dragon"));
    }

    [Test]
    public void Constructor_WithNullValue_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Keyword(null!));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("Keyword cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithEmptyValue_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Keyword(string.Empty));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("Keyword cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithWhitespace_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Keyword("   "));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("Keyword cannot be null or whitespace"));
    }

    [Test]
    public void Constructor_WithValueExceedingMaxLength_ThrowsArgumentException()
    {
        // Arrange
        var longValue = new string('a', 51);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Keyword(longValue));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("Keyword cannot exceed 50 characters"));
    }

    [Test]
    public void Constructor_WithExactlyMaxLength_Succeeds()
    {
        // Arrange
        var maxLengthValue = new string('a', 50);

        // Act
        var keyword = new Keyword(maxLengthValue);

        // Assert
        Assert.That(keyword.Value, Is.EqualTo(maxLengthValue));
        Assert.That(keyword.Value.Length, Is.EqualTo(50));
    }

    [Test]
    public void Constructor_TrimsWhitespace()
    {
        // Arrange
        const string value = "  dragon  ";

        // Act
        var keyword = new Keyword(value);

        // Assert
        Assert.That(keyword.Value, Is.EqualTo("dragon"));
    }

    [Test]
    public void Constructor_ConvertsToLowerCase()
    {
        // Arrange
        const string value = "DRAGON";

        // Act
        var keyword = new Keyword(value);

        // Assert
        Assert.That(keyword.Value, Is.EqualTo("dragon"));
    }

    [Test]
    public void Constructor_TrimsAndConvertsToLowerCase()
    {
        // Arrange
        const string value = "  DraGon  ";

        // Act
        var keyword = new Keyword(value);

        // Assert
        Assert.That(keyword.Value, Is.EqualTo("dragon"));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        var keyword1 = new Keyword("magic");
        var keyword2 = new Keyword("magic");

        // Act & Assert
        Assert.That(keyword1, Is.EqualTo(keyword2));
        Assert.That(keyword1.GetHashCode(), Is.EqualTo(keyword2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithSameValueDifferentCase_AreEqual()
    {
        // Arrange
        var keyword1 = new Keyword("magic");
        var keyword2 = new Keyword("MAGIC");

        // Act & Assert
        Assert.That(keyword1, Is.EqualTo(keyword2));
        Assert.That(keyword1.GetHashCode(), Is.EqualTo(keyword2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        var keyword1 = new Keyword("magic");
        var keyword2 = new Keyword("spells");

        // Act & Assert
        Assert.That(keyword1, Is.Not.EqualTo(keyword2));
    }

    [Test]
    public void Matches_WithExactMatch_ReturnsTrue()
    {
        // Arrange
        var keyword = new Keyword("dragon");

        // Act
        var result = keyword.Matches("dragon");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Matches_WithCaseInsensitiveMatch_ReturnsTrue()
    {
        // Arrange
        var keyword = new Keyword("dragon");

        // Act
        var result = keyword.Matches("DRAGON");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Matches_WithPartialMatch_ReturnsTrue()
    {
        // Arrange
        var keyword = new Keyword("ancient dragon");

        // Act
        var result = keyword.Matches("dragon");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Matches_WithNoMatch_ReturnsFalse()
    {
        // Arrange
        var keyword = new Keyword("dragon");

        // Act
        var result = keyword.Matches("wizard");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Matches_WithEmptySearchTerm_ReturnsTrue()
    {
        // Arrange
        var keyword = new Keyword("dragon");

        // Act
        var result = keyword.Matches(string.Empty);

        // Assert
        Assert.That(result, Is.True); // Empty string is contained in all strings
    }

    [Test]
    public void Matches_WithWhitespaceInSearchTerm_WorksCorrectly()
    {
        // Arrange
        var keyword = new Keyword("ancient dragon lore");

        // Act
        var result = keyword.Matches("dragon lore");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ImplicitConversionToString_ReturnsUnderlyingValue()
    {
        // Arrange
        var keyword = new Keyword("  DRAGON  ");

        // Act
        string result = keyword;

        // Assert
        Assert.That(result, Is.EqualTo("dragon")); // Trimmed and lowercased
    }

    [Test]
    public void ExplicitConversionFromString_CreatesValueObject()
    {
        // Arrange
        const string value = "WIZARD";

        // Act
        var keyword = (Keyword)value;

        // Assert
        Assert.That(keyword.Value, Is.EqualTo("wizard"));
    }

    [Test]
    public void ExplicitConversionFromInvalidString_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (Keyword)string.Empty);
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var keyword = new Keyword("DRAGON");

        // Act
        var result = keyword.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("dragon"));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        var keyword1 = new Keyword("dragon");
        var keyword2 = new Keyword("wizard");
        var dict = new Dictionary<Keyword, string>
        {
            [keyword1] = "Fire breathing creatures",
            [keyword2] = "Spell casting masters"
        };

        // Act & Assert
        Assert.That(dict[keyword1], Is.EqualTo("Fire breathing creatures"));
        Assert.That(dict[keyword2], Is.EqualTo("Spell casting masters"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        var keyword1 = new Keyword("dragon");
        var keyword2 = new Keyword("wizard");
        var keyword3 = new Keyword("DRAGON"); // Duplicate (case-insensitive)

        var hashSet = new HashSet<Keyword> { keyword1, keyword2, keyword3 };

        // Act & Assert
        Assert.That(hashSet.Count, Is.EqualTo(2)); // keyword3 is duplicate of keyword1
        Assert.That(hashSet.Contains(keyword1), Is.True);
        Assert.That(hashSet.Contains(keyword2), Is.True);
    }
}
