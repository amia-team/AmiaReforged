using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class ProficiencyProgressionServiceTests
{
    private ProficiencyProgressionService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ProficiencyProgressionService();
    }

    private static IndustryMembership CreateMembership(
        ProficiencyLevel level = ProficiencyLevel.Novice,
        int xpLevel = 1,
        int xp = 0)
    {
        return new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(Guid.NewGuid()),
            IndustryTag = new IndustryTag("test_industry"),
            Level = level,
            ProficiencyXpLevel = xpLevel,
            ProficiencyXp = xp,
            CharacterKnowledge = []
        };
    }

    // ==================== AwardProficiencyXp ====================

    [Test]
    public void AwardXp_SmallAmount_AccumulatesWithoutLevelUp()
    {
        IndustryMembership membership = CreateMembership(xpLevel: 1, xp: 0);
        int costForLevel1 = ProficiencyXpCurve.XpForLevel(1);

        // Award less than the cost for level 1
        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, costForLevel1 - 1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LevelsGained, Is.EqualTo(0));
        Assert.That(result.NewLevel, Is.EqualTo(1));
        Assert.That(membership.ProficiencyXp, Is.EqualTo(costForLevel1 - 1));
    }

    [Test]
    public void AwardXp_ExactCostForLevel_LevelsUpOnce()
    {
        IndustryMembership membership = CreateMembership(xpLevel: 1, xp: 0);
        int costForLevel1 = ProficiencyXpCurve.XpForLevel(1);

        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, costForLevel1);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LevelsGained, Is.EqualTo(1));
        Assert.That(result.NewLevel, Is.EqualTo(2));
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(2));
        Assert.That(membership.ProficiencyXp, Is.EqualTo(0), "Exact cost should leave 0 remainder");
    }

    [Test]
    public void AwardXp_OverflowXp_CarriesOverToNextLevel()
    {
        IndustryMembership membership = CreateMembership(xpLevel: 1, xp: 0);
        int costForLevel1 = ProficiencyXpCurve.XpForLevel(1);
        int costForLevel2 = ProficiencyXpCurve.XpForLevel(2);

        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, costForLevel1 + costForLevel2 + 50);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LevelsGained, Is.EqualTo(2));
        Assert.That(result.NewLevel, Is.EqualTo(3));
        Assert.That(membership.ProficiencyXp, Is.EqualTo(50), "Overflow XP should carry over");
    }

    [Test]
    public void AwardXp_ZeroOrNegative_ReturnsFalse()
    {
        IndustryMembership membership = CreateMembership(xpLevel: 5, xp: 10);

        ProficiencyXpResult result0 = _sut.AwardProficiencyXp(membership, 0);
        ProficiencyXpResult resultNeg = _sut.AwardProficiencyXp(membership, -10);

        Assert.That(result0.Success, Is.False);
        Assert.That(resultNeg.Success, Is.False);
        Assert.That(membership.ProficiencyXp, Is.EqualTo(10), "XP should not have changed");
    }

    // ==================== Tier Ceiling Hard Gate ====================

    [Test]
    public void AwardXp_AtTierCeiling_IsBlocked()
    {
        // Novice ceiling is level 25 — set membership to level 25 as Novice (hasn't ranked up)
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice,
            xpLevel: 25,
            xp: 0);

        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, 500);

        Assert.That(result.Success, Is.False);
        Assert.That(result.IsAtTierCeiling, Is.True);
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(25), "Level should not change");
        Assert.That(membership.ProficiencyXp, Is.EqualTo(0), "XP should not accumulate when blocked");
    }

    [Test]
    public void AwardXp_ApproachingTierCeiling_CapsAtCeiling()
    {
        // Novice at level 24 — level up should stop at 25
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice,
            xpLevel: 24,
            xp: 0);

        // Award enough XP for many levels
        int massiveXp = 100_000;
        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, massiveXp);

        Assert.That(result.Success, Is.True);
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(25), "Should stop at Novice ceiling (25)");
        Assert.That(result.IsAtTierCeiling, Is.True);
        Assert.That(membership.ProficiencyXp, Is.EqualTo(0), "Leftover XP discarded at ceiling");
    }

    [Test]
    public void AwardXp_AfterRankUp_CanContinueGaining()
    {
        // Apprentice at level 26 (just ranked up from Novice)
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Apprentice,
            xpLevel: 26,
            xp: 0);

        int cost = ProficiencyXpCurve.XpForLevel(26);
        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, cost);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LevelsGained, Is.EqualTo(1));
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(27));
    }

    // ==================== Max Level ====================

    [Test]
    public void AwardXp_AtMaxLevel_IsBlocked()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Grandmaster,
            xpLevel: 125,
            xp: 0);

        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, 500);

        Assert.That(result.Success, Is.False);
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(125));
    }

    [Test]
    public void AwardXp_MasterAtLevel124_LevelsToGrandmaster125()
    {
        // Master at level 124, about to reach 125 (Grandmaster)
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Master,
            xpLevel: 124,
            xp: 0);

        int cost = ProficiencyXpCurve.XpForLevel(124);
        ProficiencyXpResult result = _sut.AwardProficiencyXp(membership, cost);

        Assert.That(result.Success, Is.True);
        Assert.That(result.LevelsGained, Is.EqualTo(1));
        Assert.That(membership.ProficiencyXpLevel, Is.EqualTo(125));
        Assert.That(membership.Level, Is.EqualTo(ProficiencyLevel.Grandmaster),
            "Reaching level 125 should auto-promote to Grandmaster");
    }

    // ==================== CanGainXp ====================

    [Test]
    public void CanGainXp_NoviceAtLevel10_ReturnsTrue()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice, xpLevel: 10);

        Assert.That(_sut.CanGainXp(membership), Is.True);
    }

    [Test]
    public void CanGainXp_NoviceAtCeiling_ReturnsFalse()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice, xpLevel: 25);

        Assert.That(_sut.CanGainXp(membership), Is.False);
    }

    [Test]
    public void CanGainXp_GrandmasterAtMax_ReturnsFalse()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Grandmaster, xpLevel: 125);

        Assert.That(_sut.CanGainXp(membership), Is.False);
    }

    [Test]
    public void CanGainXp_ApprenticeAtLevel26_ReturnsTrue()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Apprentice, xpLevel: 26);

        Assert.That(_sut.CanGainXp(membership), Is.True);
    }

    // ==================== GetTierForLevel ====================

    [Test]
    public void GetTierForLevel_DelegatesToCurve()
    {
        Assert.That(_sut.GetTierForLevel(1), Is.EqualTo(ProficiencyLevel.Novice));
        Assert.That(_sut.GetTierForLevel(50), Is.EqualTo(ProficiencyLevel.Apprentice));
        Assert.That(_sut.GetTierForLevel(125), Is.EqualTo(ProficiencyLevel.Grandmaster));
    }

    // ==================== ProficiencyTier computed property ====================

    [Test]
    public void IndustryMembership_ProficiencyTier_MatchesXpLevel()
    {
        IndustryMembership membership = CreateMembership(
            level: ProficiencyLevel.Novice, xpLevel: 50);

        // ProficiencyTier is computed from ProficiencyXpLevel, not from the enum Level
        Assert.That(membership.ProficiencyTier, Is.EqualTo(ProficiencyLevel.Apprentice));
    }
}
