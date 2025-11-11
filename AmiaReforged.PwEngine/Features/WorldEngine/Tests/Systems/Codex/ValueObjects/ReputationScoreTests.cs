using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.Codex.ValueObjects;

[TestFixture]
public class ReputationScoreTests
{
    [Test]
    public void Constructor_WithValidValue_CreatesReputationScore()
    {
        // Arrange
        const int value = 50;

        // Act
        ReputationScore score = new ReputationScore(value);

        // Assert
        Assert.That(score.Value, Is.EqualTo(value));
    }

    [Test]
    public void Constructor_WithMinimumValue_CreatesReputationScore()
    {
        // Act
        ReputationScore score = new ReputationScore(ReputationScore.MinReputation);

        // Assert
        Assert.That(score.Value, Is.EqualTo(-100));
    }

    [Test]
    public void Constructor_WithMaximumValue_CreatesReputationScore()
    {
        // Act
        ReputationScore score = new ReputationScore(ReputationScore.MaxReputation);

        // Assert
        Assert.That(score.Value, Is.EqualTo(100));
    }

    [Test]
    public void Constructor_WithNeutralValue_CreatesReputationScore()
    {
        // Act
        ReputationScore score = new ReputationScore(ReputationScore.Neutral);

        // Assert
        Assert.That(score.Value, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithValueBelowMinimum_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new ReputationScore(-101));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("Reputation must be between -100 and 100"));
        Assert.That(ex.Message, Does.Contain("got -101"));
    }

    [Test]
    public void Constructor_WithValueAboveMaximum_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? ex = Assert.Throws<ArgumentException>(() => new ReputationScore(101));
        Assert.That(ex!.ParamName, Is.EqualTo("value"));
        Assert.That(ex.Message, Does.Contain("Reputation must be between -100 and 100"));
        Assert.That(ex.Message, Does.Contain("got 101"));
    }

    [Test]
    public void CreateNeutral_ReturnsNeutralScore()
    {
        // Act
        ReputationScore score = ReputationScore.CreateNeutral();

        // Assert
        Assert.That(score.Value, Is.EqualTo(0));
    }

    [Test]
    public void Add_WithPositiveDelta_IncreasesScore()
    {
        // Arrange
        ReputationScore score = new ReputationScore(50);

        // Act
        ReputationScore newScore = score.Add(20);

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(70));
    }

    [Test]
    public void Add_WithNegativeDelta_DecreasesScore()
    {
        // Arrange
        ReputationScore score = new ReputationScore(50);

        // Act
        ReputationScore newScore = score.Add(-30);

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(20));
    }

    [Test]
    public void Add_WithZeroDelta_ReturnsUnchangedScore()
    {
        // Arrange
        ReputationScore score = new ReputationScore(50);

        // Act
        ReputationScore newScore = score.Add(0);

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(50));
    }

    [Test]
    public void Add_WhenResultExceedsMaximum_ClampsToMaximum()
    {
        // Arrange
        ReputationScore score = new ReputationScore(90);

        // Act
        ReputationScore newScore = score.Add(50); // Would be 140 without clamping

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(100));
    }

    [Test]
    public void Add_WhenResultBelowMinimum_ClampsToMinimum()
    {
        // Arrange
        ReputationScore score = new ReputationScore(-90);

        // Act
        ReputationScore newScore = score.Add(-50); // Would be -140 without clamping

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(-100));
    }

    [Test]
    public void Add_AtMaximum_StaysAtMaximum()
    {
        // Arrange
        ReputationScore score = new ReputationScore(100);

        // Act
        ReputationScore newScore = score.Add(10);

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(100));
    }

    [Test]
    public void Add_AtMinimum_StaysAtMinimum()
    {
        // Arrange
        ReputationScore score = new ReputationScore(-100);

        // Act
        ReputationScore newScore = score.Add(-10);

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(-100));
    }

    [Test]
    public void Add_DoesNotModifyOriginalScore()
    {
        // Arrange
        ReputationScore score = new ReputationScore(50);

        // Act
        ReputationScore newScore = score.Add(20);

        // Assert
        Assert.That(score.Value, Is.EqualTo(50)); // Original unchanged
        Assert.That(newScore.Value, Is.EqualTo(70)); // New score has new value
    }

    [Test]
    public void Add_CanChainMultipleCalls()
    {
        // Arrange
        ReputationScore score = new ReputationScore(0);

        // Act
        ReputationScore newScore = score.Add(10).Add(20).Add(-5);

        // Assert
        Assert.That(newScore.Value, Is.EqualTo(25));
    }

    [Test]
    public void StructuralEquality_WithSameValue_AreEqual()
    {
        // Arrange
        ReputationScore score1 = new ReputationScore(50);
        ReputationScore score2 = new ReputationScore(50);

        // Act & Assert
        Assert.That(score1, Is.EqualTo(score2));
        Assert.That(score1.GetHashCode(), Is.EqualTo(score2.GetHashCode()));
    }

    [Test]
    public void StructuralEquality_WithDifferentValue_AreNotEqual()
    {
        // Arrange
        ReputationScore score1 = new ReputationScore(50);
        ReputationScore score2 = new ReputationScore(60);

        // Act & Assert
        Assert.That(score1, Is.Not.EqualTo(score2));
    }

    [Test]
    public void ImplicitConversionToInt_ReturnsUnderlyingValue()
    {
        // Arrange
        ReputationScore score = new ReputationScore(75);

        // Act
        int result = score;

        // Assert
        Assert.That(result, Is.EqualTo(75));
    }

    [Test]
    public void ExplicitConversionFromInt_CreatesValueObject()
    {
        // Arrange
        const int value = 80;

        // Act
        ReputationScore score = (ReputationScore)value;

        // Assert
        Assert.That(score.Value, Is.EqualTo(80));
    }

    [Test]
    public void ExplicitConversionFromInvalidInt_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _ = (ReputationScore)150);
    }

    [Test]
    public void ToString_ReturnsValue()
    {
        // Arrange
        ReputationScore score = new ReputationScore(42);

        // Act
        string result = score.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("42"));
    }

    [Test]
    public void ToString_WithNegativeValue_ReturnsNegativeString()
    {
        // Arrange
        ReputationScore score = new ReputationScore(-42);

        // Act
        string result = score.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("-42"));
    }

    [Test]
    public void CanBeUsedAsDictionaryKey()
    {
        // Arrange
        ReputationScore score1 = new ReputationScore(50);
        ReputationScore score2 = new ReputationScore(75);
        Dictionary<ReputationScore, string> dict = new Dictionary<ReputationScore, string>
        {
            [score1] = "Neutral",
            [score2] = "Friendly"
        };

        // Act & Assert
        Assert.That(dict[score1], Is.EqualTo("Neutral"));
        Assert.That(dict[score2], Is.EqualTo("Friendly"));
        Assert.That(dict.Count, Is.EqualTo(2));
    }

    [Test]
    public void CanBeUsedInHashSet()
    {
        // Arrange
        ReputationScore score1 = new ReputationScore(50);
        ReputationScore score2 = new ReputationScore(75);
        ReputationScore score3 = new ReputationScore(50); // Duplicate

        HashSet<ReputationScore> hashSet = new HashSet<ReputationScore> { score1, score2, score3 };

        // Act & Assert
        Assert.That(hashSet.Count, Is.EqualTo(2)); // score3 is duplicate of score1
        Assert.That(hashSet.Contains(score1), Is.True);
        Assert.That(hashSet.Contains(score2), Is.True);
    }

    [Test]
    public void Constants_HaveCorrectValues()
    {
        // Assert
        Assert.That(ReputationScore.MinReputation, Is.EqualTo(-100));
        Assert.That(ReputationScore.MaxReputation, Is.EqualTo(100));
        Assert.That(ReputationScore.Neutral, Is.EqualTo(0));
    }
}
