using System.Linq;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// Determines whether the trait onboarding prompt should be shown for a character.
/// </summary>
/// <remarks>
/// A character has completed trait selection once at least one of its trait records is
/// confirmed. This helper contains no NUI, no NWN event wiring, no repository access,
/// no persistence, and no DI — it is a pure, testable domain check.
/// </remarks>
public static class TraitOnboardingEligibility
{
    /// <summary>
    /// Returns <c>true</c> when the character has not yet confirmed any trait and therefore
    /// should be prompted to complete trait selection.
    /// </summary>
    public static bool IsEligible(IReadOnlyCollection<CharacterTrait> characterTraits)
    {
        return !characterTraits.Any(t => t.IsConfirmed);
    }
}
