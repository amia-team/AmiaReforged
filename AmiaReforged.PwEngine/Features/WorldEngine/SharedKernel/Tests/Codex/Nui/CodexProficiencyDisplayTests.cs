using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Nui;

[TestFixture]
public class CodexProficiencyDisplayTests
{
    [TestCase(0, "No industry memberships", "")]
    [TestCase(1, "1 industry", "Choose an industry for XP")]
    [TestCase(15, "15 industries", "Choose an industry for XP")]
    public void AllIndustries_DoesNotRepresentOneMembershipsProgress(int count, string level, string xp)
    {
        CodexProficiencyDisplay display = CodexProficiencyDisplay.Overview(count);
        Assert.That(display.LevelText, Is.EqualTo(level));
        Assert.That(display.XpText, Is.EqualTo(xp));
        Assert.That(display.Progress, Is.Zero);
        Assert.That(display.ShowProgress, Is.False);
    }

    [TestCase(0, "Layman (Lv. 0)")]
    [TestCase(1, "Novice (Lv. 1)")]
    public void ZeroXp_UsesFiniteEmptyProgressIncludingLayman(int level, string text)
    {
        CodexProficiencyDisplay display = CodexProficiencyDisplay.FromMembership(Member(level, 0));
        Assert.That(display.LevelText, Is.EqualTo(text));
        Assert.That(display.Progress, Is.Zero);
        Assert.That(display.XpText, Is.EqualTo("0 / 370 XP"));
        Assert.That(display.ShowProgress, Is.True);
    }

    [Test]
    public void XpIsRelativeToNextLevel_NotLifetimeTotal()
    {
        CodexProficiencyDisplay display = CodexProficiencyDisplay.FromMembership(Member(2, 264));
        Assert.That(display.Progress, Is.EqualTo(0.5f));
        Assert.That(display.XpText, Is.EqualTo("264 / 528 XP"));
    }

    [TestCase(-20, 0f)]
    [TestCase(370, 1f)]
    [TestCase(int.MaxValue, 1f)]
    public void ProgressRemainsWithinNativeRange(int xp, float expected)
    {
        CodexProficiencyDisplay display = CodexProficiencyDisplay.FromMembership(Member(1, xp));
        Assert.That(display.Progress, Is.EqualTo(expected));
        Assert.That(display.XpText, Is.EqualTo($"{xp} / 370 XP"));
    }

    [TestCase(125, 0)]
    [TestCase(125, 999)]
    [TestCase(126, 0)]
    public void GrandmasterIsFullWithoutDividingByZero(int level, int xp)
    {
        CodexProficiencyDisplay display = CodexProficiencyDisplay.FromMembership(Member(level, xp));
        Assert.That(display.Progress, Is.EqualTo(1f));
        Assert.That(display.XpText, Is.EqualTo("MAX"));
        Assert.That(display.ShowProgress, Is.True);
    }

    [Test]
    public void ChangingIndustryReplacesTierAndXp_ThenMissingMembershipClearsProgress()
    {
        CodexProficiencyDisplay novice = CodexProficiencyDisplay.FromMembership(Member(1, 185));
        CodexProficiencyDisplay apprentice = CodexProficiencyDisplay.FromMembership(Member(26, 0));
        Assert.That(novice.Progress, Is.EqualTo(0.5f));
        Assert.That(apprentice.LevelText, Is.EqualTo("Apprentice (Lv. 26)"));
        Assert.That(apprentice.Progress, Is.Zero);

        CodexProficiencyDisplay missing = CodexProficiencyDisplay.FromMembership(null);
        Assert.That(missing.LevelText, Is.EqualTo("Membership unavailable"));
        Assert.That(missing.Progress, Is.Zero);
        Assert.That(missing.XpText, Is.Empty);
        Assert.That(missing.ShowProgress, Is.False);
    }

    private static IndustryMembership Member(int level, int xp) => new()
    {
        CharacterId = CharacterId.From(Guid.NewGuid()), IndustryTag = new IndustryTag("smithing"),
        Level = ProficiencyLevel.Novice, CharacterKnowledge = [],
        ProficiencyXpLevel = level, ProficiencyXp = xp
    };
}
