using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services;

/// <summary>
/// This just gives the player a floaty text when the cooldown is over
/// </summary>
[ServiceBinding(typeof(CooldownNotifier))]
public class CooldownNotifier
{
    private static readonly Dictionary<string, string> CooldownEffectsByTag = new()
    {
        ["divine_wrath_cd"] = "Divine Wrath",
        ["blinding_speed_cd"] = "Blinding Speed",
        ["wlk_summon_cd"] = "Warlock Summon"
    };

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CooldownNotifier()
    {
        NwModule.Instance.OnEffectRemove += CueOnCoolDownRemove;

        Log.Info("Cooldown Notifier initialized.");
    }

    private void CueOnCoolDownRemove(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (!creature.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not { } tag) return;
        if (!CooldownEffectsByTag.TryGetValue(tag, out string? effectName)) return;

        effectName = eventData.Effect.Spell?.Name.ToString() ?? effectName;

        string abilityAvailableMessage = $"{effectName} is available!".ColorString(ColorConstants.Lime);

        player.FloatingTextString(abilityAvailableMessage, false);
    }
}

