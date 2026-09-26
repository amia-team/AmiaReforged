using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Nui.Helpers;

/// <summary>
///     Persists the per-character preference to suppress future trait-selection onboarding reminders.
/// </summary>
/// <remarks>
///     The preference is stored as a single integer local variable on the character's
///     <c>ds_pckey</c> item. Missing or zero means reminders are enabled; <c>1</c> means they are disabled.
///     This type performs no NUI, no NWN event wiring, no repository access, and no DI — it is a thin,
///     testable persistence accessor.
/// </remarks>
public static class TraitOnboardingPreference
{
    /// <summary>
    ///     The local variable name used to store the preference on <c>ds_pckey</c>.
    /// </summary>
    public const string LocalVarName = "TRAIT_SELECTION_PROMPT_DISABLED";

    /// <summary>
    ///     Returns <c>true</c> only when the preference has been explicitly disabled (local integer is <c>1</c>).
    /// </summary>
    public static bool IsDisabled(NwItem pcKey)
    {
        return pcKey.GetObjectVariable<LocalVariableInt>(LocalVarName).Value == 1;
    }

    /// <summary>
    ///     Disables future trait-selection onboarding reminders for the character by setting the
    ///     local integer to <c>1</c> on the <c>ds_pckey</c> item.
    /// </summary>
    public static void Disable(NwItem pcKey)
    {
        NWScript.SetLocalInt(pcKey, LocalVarName, 1);
    }
}
