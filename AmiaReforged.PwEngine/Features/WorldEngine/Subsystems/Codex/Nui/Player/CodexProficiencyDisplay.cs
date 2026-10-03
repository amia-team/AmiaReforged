using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;

internal sealed record CodexProficiencyDisplay(string LevelText, float Progress, string XpText, bool ShowProgress)
{
    public static CodexProficiencyDisplay Overview(int membershipCount) => membershipCount == 0
        ? new("No industry memberships", 0, "", false)
        : new($"{membershipCount} {(membershipCount == 1 ? "industry" : "industries")}", 0, "Choose an industry for XP", false);

    public static CodexProficiencyDisplay FromMembership(IndustryMembership? membership)
    {
        if (membership == null) return new("Membership unavailable", 0, "", false);

        int level = membership.ProficiencyXpLevel;
        string levelText = $"{membership.ProficiencyTier} (Lv. {level})";
        if (level >= ProficiencyXpCurve.MaxLevel) return new(levelText, 1, "MAX", true);

        // Level zero has no curve cost; retain the existing Layman display toward level one.
        int needed = ProficiencyXpCurve.XpForLevel(Math.Max(1, level));
        float progress = Math.Clamp(membership.ProficiencyXp / (float)needed, 0, 1);
        return new(levelText, progress, $"{membership.ProficiencyXp} / {needed} XP", true);
    }
}
