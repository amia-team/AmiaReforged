using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class ProficiencyXpCurveTests
{
    // ==================== XpForLevel ====================

    [Test]
    public void XpForLevel_Level1_ReturnsBaseCostPlusLogComponent()
    {
        int cost = ProficiencyXpCurve.XpForLevel(1);

        // Floor(100 + 390 * ln(2)) ≈ Floor(100 + 270.3) = 370
        Assert.That(cost, Is.GreaterThanOrEqualTo(100), "Level 1 cost should be at least BaseCost");
        Assert.That(cost, Is.GreaterThan(0));
    }

    [Test]
    public void XpForLevel_Level125_ReturnsZero_BecauseAtMaxLevel()
    {
        int cost = ProficiencyXpCurve.XpForLevel(ProficiencyXpCurve.MaxLevel);

        Assert.That(cost, Is.EqualTo(0), "At max level there is no next level to advance to");
    }

    [Test]
    public void XpForLevel_Level0_ReturnsZero()
    {
        int cost = ProficiencyXpCurve.XpForLevel(0);

        Assert.That(cost, Is.EqualTo(0), "Level 0 (Layman) has no XP cost");
    }

    [Test]
    public void XpForLevel_NegativeLevel_ReturnsZero()
    {
        int cost = ProficiencyXpCurve.XpForLevel(-5);

        Assert.That(cost, Is.EqualTo(0));
    }

    [Test]
    public void XpForLevel_IncreaseMonotonically()
    {
        int previous = 0;
        for (int level = 1; level < ProficiencyXpCurve.MaxLevel; level++)
        {
            int cost = ProficiencyXpCurve.XpForLevel(level);
            Assert.That(cost, Is.GreaterThanOrEqualTo(previous),
                $"XP cost should increase monotonically, but level {level} ({cost}) < level {level - 1} ({previous})");
            previous = cost;
        }
    }

    [Test]
    public void XpForLevel_Level124_ReturnsReasonableHighEndCost()
    {
        int cost = ProficiencyXpCurve.XpForLevel(124);

        // Should be around ~2000 based on our curve design (100 + 390*ln(125) ≈ 100 + 1882 = 1982)
        Assert.That(cost, Is.GreaterThan(1500), "Level 124 cost should be in the high range");
        Assert.That(cost, Is.LessThan(2500), "Level 124 cost should not be unreasonably high");
    }

    // ==================== TotalXpForLevel ====================

    [Test]
    public void TotalXpForLevel_Level1_ReturnsZero()
    {
        int total = ProficiencyXpCurve.TotalXpForLevel(1);

        Assert.That(total, Is.EqualTo(0), "Level 1 is the start — no accumulated XP needed");
    }

    [Test]
    public void TotalXpForLevel_Level2_EqualsXpForLevel1()
    {
        int total = ProficiencyXpCurve.TotalXpForLevel(2);
        int costForLevel1 = ProficiencyXpCurve.XpForLevel(1);

        Assert.That(total, Is.EqualTo(costForLevel1));
    }

    [Test]
    public void TotalXpForLevel_Level10_SumsCorrectly()
    {
        int expected = 0;
        for (int i = 1; i < 10; i++)
        {
            expected += ProficiencyXpCurve.XpForLevel(i);
        }

        int total = ProficiencyXpCurve.TotalXpForLevel(10);

        Assert.That(total, Is.EqualTo(expected));
    }

    // ==================== TierForLevel ====================

    [Test]
    public void TierForLevel_Level0_ReturnsLayman()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(0), Is.EqualTo(ProficiencyLevel.Layman));
    }

    [Test]
    public void TierForLevel_Level1_ReturnsNovice()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(1), Is.EqualTo(ProficiencyLevel.Novice));
    }

    [Test]
    public void TierForLevel_Level25_ReturnsNovice()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(25), Is.EqualTo(ProficiencyLevel.Novice));
    }

    [Test]
    public void TierForLevel_Level26_ReturnsApprentice()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(26), Is.EqualTo(ProficiencyLevel.Apprentice));
    }

    [Test]
    public void TierForLevel_Level50_ReturnsApprentice()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(50), Is.EqualTo(ProficiencyLevel.Apprentice));
    }

    [Test]
    public void TierForLevel_Level51_ReturnsJourneyman()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(51), Is.EqualTo(ProficiencyLevel.Journeyman));
    }

    [Test]
    public void TierForLevel_Level75_ReturnsJourneyman()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(75), Is.EqualTo(ProficiencyLevel.Journeyman));
    }

    [Test]
    public void TierForLevel_Level76_ReturnsExpert()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(76), Is.EqualTo(ProficiencyLevel.Expert));
    }

    [Test]
    public void TierForLevel_Level100_ReturnsExpert()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(100), Is.EqualTo(ProficiencyLevel.Expert));
    }

    [Test]
    public void TierForLevel_Level101_ReturnsMaster()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(101), Is.EqualTo(ProficiencyLevel.Master));
    }

    [Test]
    public void TierForLevel_Level124_ReturnsMaster()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(124), Is.EqualTo(ProficiencyLevel.Master));
    }

    [Test]
    public void TierForLevel_Level125_ReturnsGrandmaster()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(125), Is.EqualTo(ProficiencyLevel.Grandmaster));
    }

    [Test]
    public void TierForLevel_NegativeLevel_ReturnsLayman()
    {
        Assert.That(ProficiencyXpCurve.TierForLevel(-1), Is.EqualTo(ProficiencyLevel.Layman));
    }

    // ==================== CeilingForTier ====================

    [Test]
    public void CeilingForTier_Novice_Returns25()
    {
        Assert.That(ProficiencyXpCurve.CeilingForTier(ProficiencyLevel.Novice), Is.EqualTo(25));
    }

    [Test]
    public void CeilingForTier_Apprentice_Returns50()
    {
        Assert.That(ProficiencyXpCurve.CeilingForTier(ProficiencyLevel.Apprentice), Is.EqualTo(50));
    }

    [Test]
    public void CeilingForTier_Journeyman_Returns75()
    {
        Assert.That(ProficiencyXpCurve.CeilingForTier(ProficiencyLevel.Journeyman), Is.EqualTo(75));
    }

    [Test]
    public void CeilingForTier_Expert_Returns100()
    {
        Assert.That(ProficiencyXpCurve.CeilingForTier(ProficiencyLevel.Expert), Is.EqualTo(100));
    }

    [Test]
    public void CeilingForTier_Master_Returns124()
    {
        Assert.That(ProficiencyXpCurve.CeilingForTier(ProficiencyLevel.Master), Is.EqualTo(124));
    }

    [Test]
    public void CeilingForTier_Grandmaster_Returns125()
    {
        Assert.That(ProficiencyXpCurve.CeilingForTier(ProficiencyLevel.Grandmaster), Is.EqualTo(125));
    }

    [Test]
    public void CeilingForTier_Layman_Returns0()
    {
        Assert.That(ProficiencyXpCurve.CeilingForTier(ProficiencyLevel.Layman), Is.EqualTo(0));
    }

    // ==================== FloorForTier ====================

    [Test]
    public void FloorForTier_Novice_Returns1()
    {
        Assert.That(ProficiencyXpCurve.FloorForTier(ProficiencyLevel.Novice), Is.EqualTo(1));
    }

    [Test]
    public void FloorForTier_Apprentice_Returns26()
    {
        Assert.That(ProficiencyXpCurve.FloorForTier(ProficiencyLevel.Apprentice), Is.EqualTo(26));
    }

    [Test]
    public void FloorForTier_Grandmaster_Returns125()
    {
        Assert.That(ProficiencyXpCurve.FloorForTier(ProficiencyLevel.Grandmaster), Is.EqualTo(125));
    }
}
