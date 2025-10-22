using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.Entities;

[TestFixture]
public class FactionReputationTests
{
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _testDate = new DateTime(2025, 10, 22, 12, 0, 0);
    }

    #region Construction Tests

    [Test]
    public void Constructor_WithValidRequiredProperties_CreatesInstance()
    {
        // Arrange & Act
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_001"),
            FactionName = "The Silver Order",
            DateEstablished = _testDate
        };

        // Assert
        Assert.That(reputation.FactionId.Value, Is.EqualTo("faction_001"));
        Assert.That(reputation.FactionName, Is.EqualTo("The Silver Order"));
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(0));
        Assert.That(reputation.DateEstablished, Is.EqualTo(_testDate));
        Assert.That(reputation.LastChanged, Is.EqualTo(_testDate));
    }

    [Test]
    public void Constructor_WithOptionalDescription_SetsDescription()
    {
        // Arrange & Act
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_002"),
            FactionName = "The Dark Brotherhood",
            DateEstablished = _testDate,
            Description = "A secret organization of assassins"
        };

        // Assert
        Assert.That(reputation.Description, Is.EqualTo("A secret organization of assassins"));
    }

    [Test]
    public void Constructor_WithoutDescription_HasNullDescription()
    {
        // Arrange & Act
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_003"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Assert
        Assert.That(reputation.Description, Is.Null);
    }

    [Test]
    public void Constructor_InitialHistory_IsEmpty()
    {
        // Arrange & Act
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_004"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Assert
        Assert.That(reputation.History, Is.Not.Null);
        Assert.That(reputation.History, Is.Empty);
    }

    #endregion

    #region AdjustReputation Tests

    [Test]
    public void AdjustReputation_WithPositiveDelta_IncreasesScore()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_005"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };
        DateTime adjustDate = _testDate.AddHours(1);

        // Act
        reputation.AdjustReputation(10, "Helped a citizen", adjustDate);

        // Assert
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(10));
        Assert.That(reputation.LastChanged, Is.EqualTo(adjustDate));
    }

    [Test]
    public void AdjustReputation_WithNegativeDelta_DecreasesScore()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_006"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };
        DateTime adjustDate = _testDate.AddHours(1);

        // Act
        reputation.AdjustReputation(-15, "Attacked a guard", adjustDate);

        // Assert
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(-15));
        Assert.That(reputation.LastChanged, Is.EqualTo(adjustDate));
    }

    [Test]
    public void AdjustReputation_WithZeroDelta_DoesNothing()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(25), _testDate)
        {
            FactionId = new FactionId("faction_007"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };
        DateTime adjustDate = _testDate.AddHours(1);

        // Act
        reputation.AdjustReputation(0, "No change", adjustDate);

        // Assert
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(25));
        Assert.That(reputation.LastChanged, Is.EqualTo(_testDate)); // Unchanged
        Assert.That(reputation.History, Is.Empty); // No history entry added
    }

    [Test]
    public void AdjustReputation_MultipleTimes_AccumulatesChanges()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_008"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act
        reputation.AdjustReputation(10, "Good deed 1", _testDate.AddHours(1));
        reputation.AdjustReputation(15, "Good deed 2", _testDate.AddHours(2));
        reputation.AdjustReputation(-5, "Minor offense", _testDate.AddHours(3));

        // Assert
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(20));
    }

    [Test]
    public void AdjustReputation_AboveMaximum_ClampsToMaximum()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(90), _testDate)
        {
            FactionId = new FactionId("faction_009"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act
        reputation.AdjustReputation(50, "Huge heroic deed", _testDate.AddHours(1));

        // Assert
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(100)); // Clamped to max
    }

    [Test]
    public void AdjustReputation_BelowMinimum_ClampsToMinimum()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(-90), _testDate)
        {
            FactionId = new FactionId("faction_010"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act
        reputation.AdjustReputation(-50, "Terrible crime", _testDate.AddHours(1));

        // Assert
        Assert.That(reputation.CurrentScore.Value, Is.EqualTo(-100)); // Clamped to min
    }

    #endregion

    #region History Tracking Tests

    [Test]
    public void AdjustReputation_AddsEntryToHistory()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_011"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };
        DateTime adjustDate = _testDate.AddHours(1);

        // Act
        reputation.AdjustReputation(10, "Helped citizen", adjustDate);

        // Assert
        Assert.That(reputation.History, Has.Count.EqualTo(1));
        ReputationChange entry = reputation.History[0];
        Assert.That(entry.Timestamp, Is.EqualTo(adjustDate));
        Assert.That(entry.Delta, Is.EqualTo(10));
        Assert.That(entry.OldScore.Value, Is.EqualTo(0));
        Assert.That(entry.NewScore.Value, Is.EqualTo(10));
        Assert.That(entry.Reason, Is.EqualTo("Helped citizen"));
    }

    [Test]
    public void AdjustReputation_MultipleChanges_AddsMultipleHistoryEntries()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_012"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act
        reputation.AdjustReputation(10, "First action", _testDate.AddHours(1));
        reputation.AdjustReputation(5, "Second action", _testDate.AddHours(2));
        reputation.AdjustReputation(-3, "Third action", _testDate.AddHours(3));

        // Assert
        Assert.That(reputation.History, Has.Count.EqualTo(3));
        Assert.That(reputation.History[0].Reason, Is.EqualTo("First action"));
        Assert.That(reputation.History[1].Reason, Is.EqualTo("Second action"));
        Assert.That(reputation.History[2].Reason, Is.EqualTo("Third action"));
    }

    [Test]
    public void AdjustReputation_WithClamping_RecordsClampedValue()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(90), _testDate)
        {
            FactionId = new FactionId("faction_013"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act
        reputation.AdjustReputation(50, "Exceeds maximum", _testDate.AddHours(1));

        // Assert
        ReputationChange entry = reputation.History[0];
        Assert.That(entry.OldScore.Value, Is.EqualTo(90));
        Assert.That(entry.NewScore.Value, Is.EqualTo(100)); // Clamped
        Assert.That(entry.Delta, Is.EqualTo(50)); // Original delta preserved
    }

    [Test]
    public void History_IsReadOnly()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(0), _testDate)
        {
            FactionId = new FactionId("faction_014"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.History, Is.InstanceOf<IReadOnlyList<ReputationChange>>());
    }

    #endregion

    #region GetStanding Tests

    [Test]
    public void GetStanding_WithExaltedScore_ReturnsExalted()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(75), _testDate)
        {
            FactionId = new FactionId("faction_015"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Exalted"));

        reputation.AdjustReputation(25, "More rep", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Exalted"));
    }

    [Test]
    public void GetStanding_WithReveredScore_ReturnsRevered()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(50), _testDate)
        {
            FactionId = new FactionId("faction_016"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Revered"));

        reputation.AdjustReputation(24, "More rep", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Revered"));
    }

    [Test]
    public void GetStanding_WithHonoredScore_ReturnsHonored()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(25), _testDate)
        {
            FactionId = new FactionId("faction_017"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Honored"));

        reputation.AdjustReputation(24, "More rep", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Honored"));
    }

    [Test]
    public void GetStanding_WithFriendlyScore_ReturnsFriendly()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(10), _testDate)
        {
            FactionId = new FactionId("faction_018"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Friendly"));

        reputation.AdjustReputation(14, "More rep", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Friendly"));
    }

    [Test]
    public void GetStanding_WithNeutralScore_ReturnsNeutral()
    {
        // Arrange & Act & Assert
        int[] neutralScores = new[] { -9, -5, 0, 5, 9 };
        foreach (int score in neutralScores)
        {
            FactionReputation reputation = new FactionReputation(new ReputationScore(score), _testDate)
            {
                FactionId = new FactionId($"faction_neutral_{score}"),
                FactionName = "Test Faction",
                DateEstablished = _testDate
            };

            Assert.That(reputation.GetStanding(), Is.EqualTo("Neutral"),
                $"Score {score} should be Neutral");
        }
    }

    [Test]
    public void GetStanding_WithUnfriendlyScore_ReturnsUnfriendly()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(-10), _testDate)
        {
            FactionId = new FactionId("faction_019"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Unfriendly"));

        reputation.AdjustReputation(-14, "More negative", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Unfriendly"));
    }

    [Test]
    public void GetStanding_WithHostileScore_ReturnsHostile()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(-26), _testDate)
        {
            FactionId = new FactionId("faction_020"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Hostile"));

        reputation.AdjustReputation(-23, "More negative", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Hostile"));
    }

    [Test]
    public void GetStanding_WithHatedScore_ReturnsHated()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(-51), _testDate)
        {
            FactionId = new FactionId("faction_021"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Hated"));

        reputation.AdjustReputation(-23, "More negative", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Hated"));
    }

    [Test]
    public void GetStanding_WithNemesisScore_ReturnsNemesis()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(-75), _testDate)
        {
            FactionId = new FactionId("faction_022"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.GetStanding(), Is.EqualTo("Nemesis"));

        reputation.AdjustReputation(-25, "More negative", _testDate);
        Assert.That(reputation.GetStanding(), Is.EqualTo("Nemesis"));
    }

    [Test]
    public void GetStanding_AtBoundaries_ReturnsCorrectStanding()
    {
        // Test boundary values for each standing
        (int, string)[] testCases = new[]
        {
            (75, "Exalted"),
            (50, "Revered"),
            (25, "Honored"),
            (10, "Friendly"),
            (9, "Neutral"),
            (0, "Neutral"),
            (-9, "Neutral"),
            (-10, "Unfriendly"),   // -10 is NOT > -10, boundary is exclusive
            (-11, "Unfriendly"),
            (-24, "Unfriendly"),
            (-25, "Hostile"),   // -25 is NOT > -25, boundary is exclusive
            (-49, "Hostile"),
            (-50, "Hated"),     // -50 is NOT > -50, boundary is exclusive
            (-74, "Hated"),
            (-75, "Nemesis"),   // -75 is NOT > -75, boundary is exclusive
            (-100, "Nemesis")
        };

        foreach ((int score, string expectedStanding) in testCases)
        {
            FactionReputation reputation = new FactionReputation(new ReputationScore(score), _testDate)
            {
                FactionId = new FactionId($"faction_boundary_{score}"),
                FactionName = "Test Faction",
                DateEstablished = _testDate
            };

            Assert.That(reputation.GetStanding(), Is.EqualTo(expectedStanding),
                $"Score {score} should be {expectedStanding}");
        }
    }

    #endregion

    #region IsAtLeast Tests

    [Test]
    public void IsAtLeast_WithScoreAboveThreshold_ReturnsTrue()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(50), _testDate)
        {
            FactionId = new FactionId("faction_023"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.IsAtLeast(40), Is.True);
        Assert.That(reputation.IsAtLeast(25), Is.True);
        Assert.That(reputation.IsAtLeast(0), Is.True);
    }

    [Test]
    public void IsAtLeast_WithScoreEqualToThreshold_ReturnsTrue()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(50), _testDate)
        {
            FactionId = new FactionId("faction_024"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.IsAtLeast(50), Is.True);
    }

    [Test]
    public void IsAtLeast_WithScoreBelowThreshold_ReturnsFalse()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(30), _testDate)
        {
            FactionId = new FactionId("faction_025"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.IsAtLeast(40), Is.False);
        Assert.That(reputation.IsAtLeast(50), Is.False);
        Assert.That(reputation.IsAtLeast(100), Is.False);
    }

    [Test]
    public void IsAtLeast_WithNegativeScores_WorksCorrectly()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(-30), _testDate)
        {
            FactionId = new FactionId("faction_026"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.IsAtLeast(-40), Is.True);
        Assert.That(reputation.IsAtLeast(-30), Is.True);
        Assert.That(reputation.IsAtLeast(-20), Is.False);
        Assert.That(reputation.IsAtLeast(0), Is.False);
    }

    #endregion

    #region IsAtMost Tests

    [Test]
    public void IsAtMost_WithScoreBelowThreshold_ReturnsTrue()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(30), _testDate)
        {
            FactionId = new FactionId("faction_027"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.IsAtMost(40), Is.True);
        Assert.That(reputation.IsAtMost(50), Is.True);
        Assert.That(reputation.IsAtMost(100), Is.True);
    }

    [Test]
    public void IsAtMost_WithScoreEqualToThreshold_ReturnsTrue()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(50), _testDate)
        {
            FactionId = new FactionId("faction_028"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.IsAtMost(50), Is.True);
    }

    [Test]
    public void IsAtMost_WithScoreAboveThreshold_ReturnsFalse()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(60), _testDate)
        {
            FactionId = new FactionId("faction_029"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        Assert.That(reputation.IsAtMost(50), Is.False);
        Assert.That(reputation.IsAtMost(40), Is.False);
        Assert.That(reputation.IsAtMost(0), Is.False);
    }

    [Test]
    public void IsAtMost_WithNegativeScores_WorksCorrectly()
    {
        // Arrange
        FactionReputation reputation = new FactionReputation(new ReputationScore(-30), _testDate)
        {
            FactionId = new FactionId("faction_030"),
            FactionName = "Test Faction",
            DateEstablished = _testDate
        };

        // Act & Assert
        // Score is -30
        Assert.That(reputation.IsAtMost(-20), Is.True);  // -30 <= -20? Yes, -30 is less than -20
        Assert.That(reputation.IsAtMost(-30), Is.True);  // -30 <= -30? Yes
        Assert.That(reputation.IsAtMost(-40), Is.False); // -30 <= -40? No, -30 > -40
        Assert.That(reputation.IsAtMost(0), Is.True);    // -30 <= 0? Yes
    }

    #endregion
}
