using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard;

/// <summary>
/// Service that restores the player's collision bubble preference on login.
/// Checks the PC Key for the EffectGhost variable and applies the cutscene ghost effect if needed.
/// </summary>
[ServiceBinding(typeof(CollisionBubbleService))]
public class CollisionBubbleService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string PcKeyResRef = "ds_pckey";
    private const string EffectGhostVar = "EffectGhost";

    public CollisionBubbleService()
    {
        NwModule.Instance.OnClientEnter += OnClientEnter;
    }

    private void OnClientEnter(ModuleEvents.OnClientEnter obj)
    {
        NwPlayer player = obj.Player;
        NwCreature? creature = player.LoginCreature;

        if (creature == null) return;

        // Find the PC Key
        NwItem? pcKey = creature.Inventory.Items.FirstOrDefault(i => i.ResRef == PcKeyResRef);
        if (pcKey == null) return;

        // Check the saved preference
        int effectGhostValue = pcKey.GetObjectVariable<LocalVariableInt>(EffectGhostVar).Value;

        // If EffectGhost is 1, the player had their bubble toggled OFF (ghost effect applied)
        // So we need to re-apply the ghost effect on login
        if (effectGhostValue == 1)
        {
            // Check if they already have the ghost effect (shouldn't happen, but just in case)
            bool hasGhostEffect = false;
            foreach (Effect effect in creature.ActiveEffects)
            {
                if (effect.EffectType == EffectType.CutsceneGhost)
                {
                    hasGhostEffect = true;
                    break;
                }
            }

            if (!hasGhostEffect)
            {
                Effect ghostEffect = Effect.CutsceneGhost();
                creature.ApplyEffect(EffectDuration.Permanent, ghostEffect);
                Log.Debug("Restored collision bubble preference (bubble OFF) for player {Player}.", player.LoginCreature?.Name ?? "Unknown");
            }
        }
    }
}

