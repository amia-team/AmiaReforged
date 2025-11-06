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
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public CooldownNotifier()
    {
        NwModule.Instance.OnEffectRemove += OnCooldownComplete;

        Log.Info("Cooldown Notifier initialized.");
    }

    private void OnCooldownComplete(OnEffectRemove eventData)
    {
        if (eventData.Object is not NwCreature creature) return;
        if (!creature.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Effect.Tag is not { } tag || !tag.EndsWith("_cd")) return;

        string effectName = eventData.Effect.Spell?.Name.ToString() ?? "Unknown";

        string abilityAvailableMessage = $"{effectName} is available!".ColorString(ColorConstants.Lime);

        player.FloatingTextString(abilityAvailableMessage, false);
    }
}

