using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CasterWeaponForge))]
public class CasterWeaponForge
{
    private const string ForgeTag = "caster_weapon_forge";

    public CasterWeaponForge()
    {
        IEnumerable<NwPlaceable> forges = NwObject.FindObjectsWithTag<NwPlaceable>(ForgeTag);

        foreach (NwPlaceable forge in forges)
        {
            forge.OnSpellCastAt += EnchantWeapon;
        }
    }

    private void EnchantWeapon(PlaceableEvents.OnSpellCastAt obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        if (obj.Spell.SpellType != Spell.GreaterMagicWeapon)
        {
            player.SendServerMessage("You can only enchant weapons here with Greater Magic Weapon.");
            return;
        }

        if (obj.Placeable.Inventory.Items.Count() != 1)
        {
            player.SendServerMessage("You can only enchant one weapon at a time.");
            return;
        }

        NwItem weapon = obj.Placeable.Inventory.Items.ToArray()[0];
        int baseItemType = NWScript.GetBaseItemType(weapon);

        if (!ItemTypeConstants.Melee2HWeapons().Contains(baseItemType) ||
            !ItemTypeConstants.MeleeWeapons().Contains(baseItemType))
        {
            player.SendServerMessage("You can only enchant melee weapons here.");
            return;
        }

        if (NWScript.GetLocalInt(weapon, "CASTER_WEAPON") == 1)
        {
            player.SendServerMessage("This weapon is already enchanted.");
            return;
        }

        NWScript.SetLocalInt(weapon, "CASTER_WEAPON", 1);
    }
}