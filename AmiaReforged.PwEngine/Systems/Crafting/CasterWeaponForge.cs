using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;
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
    public const string LocalIntCasterWeapon = "CASTER_WEAPON";

    private List<Spell> _spellWhiteList =
    [
        Spell.Restoration,
        Spell.LightningBolt,
        Spell.HealingCircle
    ];

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
        if (!obj.OpenedBy.IsPlayerControlled(out NwPlayer? player)) return;

        if (player.LoginCreature == null)
        {
            LogManager.GetCurrentClassLogger().Info(message: "Player login creature is null.");
            return;
        }

        NwItem? pcKey = player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.Tag == "ds_pckey");

        if (pcKey == null)
        {
            LogManager.GetCurrentClassLogger().Info(message: "PC key is null.");
            return;
        }

        if (NWScript.GetLocalInt(pcKey, sVarName: "ignore_caster_forge") == 1) return;

        GenericWindow
            .Builder()
            .For()
            .SimplePopup()
            .WithPlayer(player)
            .WithTitle(title: "Caster Weapon Forge")
            .WithMessage(
                "You can turn a blank weapon into a caster weapon here by casting any level 3 or higher spell on it. "
                + "This will prevent any typical weapon properties from being placed on the item. These weapons will not "
                + "accept any greater magic weapon or flame weapon enchantments."
                + " One-handed weapons have 12 powers, Two-handed weapons have 20.")
            .EnableIgnoreButton(ignoreTag: "ignore_caster_forge")
            .Open();
    }

    private void EnchantWeapon(PlaceableEvents.OnSpellCastAt obj)
    {
        if (!obj.Caster.IsPlayerControlled(out NwPlayer? player)) return;

        if (obj.Spell.InnateSpellLevel < 3) // Only allow level 3 spells
        {
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(player)
                .WithTitle(title: "Caster Weapon Forge")
                .WithMessage(message: "You must cast a level 3 or higher spell.")
                .Open();

            return;
        }

        if (obj.Placeable.Inventory.Items.Count() != 1)
        {
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(player)
                .WithTitle(title: "Caster Weapon Forge")
                .WithMessage(message: "You can only enchant one weapon at a time.")
                .Open();

            return;
        }

        NwItem weapon = obj.Placeable.Inventory.Items.ToArray()[0];
        int baseItemType = NWScript.GetBaseItemType(weapon);

        List<int> weapons = ItemTypeConstants.MeleeWeapons();
        List<int> melee2HWeapons = ItemTypeConstants.Melee2HWeapons();

        weapons.AddRange(melee2HWeapons);

        if (!weapons.Contains(baseItemType))
        {
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(player)
                .WithTitle(title: "Caster Weapon Forge")
                .WithMessage(message: "You can only enchant melee weapons.")
                .Open();

            return;
        }

        if (NWScript.GetLocalInt(weapon, sVarName: LocalIntCasterWeapon) == 1)
        {
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(player)
                .WithTitle(title: "Caster Weapon Forge")
                .WithMessage(message: "This weapon is already enchanted.")
                .Open();

            return;
        }

        // TODO: Factor in material, quality, and other flavor properties
        if (weapon.ItemProperties.Any())
        {
            GenericWindow
                .Builder()
                .For()
                .SimplePopup()
                .WithPlayer(player)
                .WithTitle(title: "Caster Weapon Forge")
                .WithMessage(message: "You cannot enchant a weapon that already has properties on it.")
                .Open();

            return;
        }

        Effect visualEffect = Effect.VisualEffect(VfxType.ImpBlindDeafM);
        obj.Placeable.ApplyEffect(EffectDuration.Instant, visualEffect);
        NWScript.SetLocalInt(weapon, sVarName: LocalIntCasterWeapon, 1);
    }
}