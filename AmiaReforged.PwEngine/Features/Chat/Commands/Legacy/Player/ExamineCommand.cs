using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Chat.Commands.Legacy.Player;

/// <summary>
/// Examine a creature hide or natural weapon equipped on yourself.
/// Ported from f_Examine() in mod_pla_cmd.nss.
/// Usage: ./examine [rclaw|lclaw|bite|hand] (default: creature armor)
/// </summary>
[ServiceBinding(typeof(IChatCommand))]
public class ExamineCommand : IChatCommand
{
    public string Command => "./examine";
    public string Description => "Examine your creature hide/claws: [rclaw|lclaw|bite|hand]";
    public string AllowedRoles => "Player";

    public async Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        await NwTask.SwitchToMainThread();

        NwCreature? creature = caller.LoginCreature;
        if (creature == null) return;

        string slotName = args.Length > 0 ? args[0].ToLowerInvariant() : "";

        InventorySlot slot = slotName switch
        {
            "rclaw" => InventorySlot.CreatureRightWeapon,
            "lclaw" => InventorySlot.CreatureLeftWeapon,
            "bite" => InventorySlot.CreatureBiteWeapon,
            "hand" => InventorySlot.RightHand,
            _ => InventorySlot.CreatureSkin
        };

        NwItem? item = creature.GetItemInSlot(slot);
        if (item == null)
        {
            caller.SendServerMessage("You don't have a creature hide/weapon in that slot!", ColorConstants.Orange);
            return;
        }

        // Use raw NWScript ActionExamine since Anvil doesn't expose it directly
        NWScript.AssignCommand(creature, () => NWScript.ActionExamine(item));
    }
}
