﻿using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(CasterWeaponForge))]
public class CasterWeaponForge
{
    private const string ForgeTag = "caster_weapon_forge";

    private List<Spell> _spellWhiteList = new List<Spell>
    {
        Spell.Restoration,
        Spell.IsaacsLesserMissileStorm,
        Spell.IsaacsGreaterMissileStorm,
        Spell.HealingCircle
    };

    public CasterWeaponForge()
    {
        NwPlaceable[] forges = NwObject.FindObjectsWithTag<NwPlaceable>(ForgeTag).ToArray();
        LogManager.GetCurrentClassLogger().Info("Number of forges found: " + forges.Length);
        foreach (NwPlaceable forge in forges)
        {
            forge.OnSpellCastAt += EnchantWeapon;
            forge.OnOpen += OpenPopup;
        }
    }

    private void OpenPopup(PlaceableEvents.OnOpen obj)
    {
        if (!obj.OpenedBy.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        if (player.LoginCreature == null)
        {
            LogManager.GetCurrentClassLogger().Info("Player login creature is null.");
            return;
        }

        NwItem? pcKey = player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.Tag == "ds_pckey");

        if (pcKey == null)
        {
            LogManager.GetCurrentClassLogger().Info("PC key is null.");
            return;
        }

        if (NWScript.GetLocalInt(pcKey, "ignore_caster_forge") == 1)
        {
            return;
        }

        GenericWindow
            .Builder()
            .For()
            .SimplePopup()
            .WithPlayer(player)
            .WithTitle("Caster Weapon Forge")
            .WithMessage(
                "You can turn a blank weapon into a caster weapon here by casting Restoration, any Missile Storm,"
                + " or Healing Circle."
                + " This will prevent any typical weapon properties from being placed on the item. These weapons will not "
                + "accept any greater magic weapon or flame weapon enchantments."
                + " One-handed weapons have 12 powers, Two-handed weapons have 20.")
            .EnableIgnoreButton("ignore_caster_forge")
            .Open();
    }

    private void EnchantWeapon(PlaceableEvents.OnSpellCastAt obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        if (!_spellWhiteList.Contains(obj.Spell.SpellType))
        {
            player.SendServerMessage(
                "You can only enchant weapons here with: Restoration, Isaac's Lesser Missile Storm, Isaac's Greater Missile, or Healing Circle.");
            return;
        }

        if (obj.Placeable.Inventory.Items.Count() != 1)
        {
            player.SendServerMessage("You can only enchant one weapon at a time.");
            return;
        }

        NwItem weapon = obj.Placeable.Inventory.Items.ToArray()[0];
        int baseItemType = NWScript.GetBaseItemType(weapon);

        // combines weapons
        List<int> weapons = ItemTypeConstants.MeleeWeapons();
        List<int> melee2HWeapons = ItemTypeConstants.Melee2HWeapons();

        weapons.AddRange(melee2HWeapons);

        if (!weapons.Contains(baseItemType))
        {
            player.SendServerMessage("You can only enchant one-handed or two-handed melee weapons.");
            return;
        }

        if (NWScript.GetLocalInt(weapon, "CASTER_WEAPON") == 1)
        {
            player.SendServerMessage("This weapon is already enchanted.");
            return;
        }

        // No properties may be present on the weapon
        if (weapon.ItemProperties.Any())
        {
            player.SendServerMessage("This weapon already has properties.");
            return;
        }

        NWScript.SetLocalInt(weapon, "CASTER_WEAPON", 1);
    }
}