using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.Feats;

[ServiceBinding((typeof(ArmoredCaster)))]
public class ArmoredCaster
{
    private const VfxType SpellFailHeadVfx = (VfxType)292;
    private const VfxType SpellFailHandVfx = (VfxType)293;

    public ArmoredCaster()
    {
        NwModule.Instance.OnSpellCast += CheckArmoredCaster;
    }

    private void CheckArmoredCaster(OnSpellCast eventData)
    {
        if (eventData.Caster is not NwCreature warlock) return;
        if (warlock.ArcaneSpellFailure <= 0) return;
        if (warlock.Classes[eventData.ClassIndex].Class != WarlockConstants.WarlockClass) return;
        if (eventData.Spell?.SpellComponents is SpellComponents.None or SpellComponents.Verbal) return;

        bool majorityLevelWarlock = warlock.CasterLevel > warlock.Level / 2;
        bool hasValidOffhand = warlock.GetItemInSlot(InventorySlot.LeftHand)?
            .BaseItem.ItemType is not BaseItemType.LargeShield and not BaseItemType.TowerShield;
        bool hasLightArmor = warlock.GetItemInSlot(InventorySlot.Chest)?
            .ACValue is not > 3;

        bool qualifiesForArmoredCaster = majorityLevelWarlock && hasValidOffhand && hasLightArmor;

        int effectiveAsf = warlock.ArcaneSpellFailure;

        if (qualifiesForArmoredCaster)
        {
            byte? armorAsf = warlock.GetItemInSlot(InventorySlot.Chest)?.BaseItem.ArcaneSpellFailure;
            if (armorAsf != null)
                effectiveAsf -= (int)armorAsf;

            byte? shieldAsf = warlock.GetItemInSlot(InventorySlot.LeftHand)?.BaseItem.ArcaneSpellFailure;
            if (shieldAsf != null)
                effectiveAsf -= (int)shieldAsf;
        }

        if (effectiveAsf <= 0) return;

        if (Random.Shared.Roll(100) > effectiveAsf) return;

        eventData.PreventSpellCast = true;

        VfxType spellFailVfx = SpellFailHandVfx;
        if (eventData.Spell?.CastAnim == SpellCastAnimType.Up)
            spellFailVfx = SpellFailHeadVfx;

        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(spellFailVfx));

        if (warlock.IsPlayerControlled(out NwPlayer? player))
            player.SendServerMessage("Arcane spell failure!");
    }
}
