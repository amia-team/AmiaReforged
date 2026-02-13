using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Removes all positive buff effects from the player and temporary item properties from equipped items.
/// Ported from f_debuff / SafeDebuff() in mod_pla_cmd.nss.
/// Usage: ./debuff (or f_debuff)
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class DebuffCommand : IChatCommand
{
    public string Command => "./debuff";
    public string Description => "Remove all positive buffs from yourself";
    public string AllowedRoles => "All";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        int removedEffects = 0;

        // Remove buff spell effects
        foreach (Effect effect in creature.ActiveEffects.ToList())
        {
            int spellId = effect.Spell?.Id ?? -1;
            if (spellId >= 0 && SpellBuffWhitelist.IsBuffSpell(spellId))
            {
                creature.RemoveEffect(effect);
                removedEffects++;
            }
        }

        // Remove temporary item properties from all equipped items
        int removedProps = 0;
        for (int slot = 0; slot < 18; slot++) // NUM_INVENTORY_SLOTS = 18
        {
            NwItem? item = creature.GetItemInSlot((InventorySlot)slot);
            if (item == null) continue;

            foreach (ItemProperty ip in item.ItemProperties.ToList())
            {
                if (ip.DurationType == EffectDuration.Temporary)
                {
                    item.RemoveItemProperty(ip);
                    removedProps++;
                }
            }
        }

        caller.SendServerMessage(
            $"Debuff complete: removed {removedEffects} buff effect(s) and {removedProps} temporary item property/ies.",
            ColorConstants.Lime);
    }
}
