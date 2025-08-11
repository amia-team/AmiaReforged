using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NLog;
using NWN.Core;

namespace AmiaReforged.Classes.EffectUtils;

/// <summary>
/// This just gives the player a floaty text when the cooldown is over
/// </summary>
[ServiceBinding(typeof(CueCoolDownRemoval))]
public class CueCoolDownRemoval
{
    private static readonly Dictionary<string, string> CoolDownEffectNamesByTag = new()
    {
        ["divine_wrath_cd"] = "Divine Wrath",
        ["blinding_speed_cd"] = "Blinding Speed",
        ["wlk_summon_cd"] = "Warlock Summon"
    };

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CueCoolDownRemoval()
    {
        NwModule.Instance.OnEffectRemove += CueOnCoolDownRemove;

        Log.Info("Cue Cool Down Removal initialized.");
    }

    private void CueOnCoolDownRemove(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (!creature.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not { } tag) return;
        if (!CoolDownEffectNamesByTag.TryGetValue(tag, out string? effectName)) return;

        effectName = eventData.Effect.Spell?.Name.ToString() ?? effectName;

        string abilityAvailableMessage = $"{effectName} is available!".ColorString(ColorConstants.Green);

        player.FloatingTextString(abilityAvailableMessage, false, false);
    }
}

