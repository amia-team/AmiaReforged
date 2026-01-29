using Anvil.API;

namespace AmiaReforged.Classes.Duelist;

public static class DuelistBonusEffect
{
    private static string DuelistEffectTag => "duelist_bonus";
    public static void ApplyDuelistBonusEffect(NwCreature creature, int duelistLevel)
    {
        Effect? existingDuelistEffect =
            creature.ActiveEffects.FirstOrDefault(e => e.Tag != null && e.Tag == DuelistEffectTag);

        bool isWieldingOneHanded = creature.GetItemInSlot(InventorySlot.RightHand) is { IsRangedWeapon: false } weapon
            && (int)creature.Size >= (int)weapon.BaseItem.WeaponSize;

        NwPlayer? player = creature.ControllingPlayer;

        if (!isWieldingOneHanded)
        {
            if (existingDuelistEffect == null) return;

            creature.RemoveEffect(existingDuelistEffect);
            player?.SendServerMessage("Removed Duelist bonus effect.");

            return;
        }

        if (existingDuelistEffect != null) return;

        Effect duelistBonusEffect = GetDuelistBonusEffect(duelistLevel, out string message);
        duelistBonusEffect.SubType = EffectSubType.Unyielding;
        duelistBonusEffect.Tag = DuelistEffectTag;

        creature.ApplyEffect(EffectDuration.Permanent, duelistBonusEffect);

        player?.SendServerMessage(message);
    }

    private static Effect GetDuelistBonusEffect(int duelistLevel, out string message)
    {
        int bonusAc = duelistLevel switch
        {
            1 => 1,
            2 => 3,
            3 => 4,
            4 => 5,
            5 => 7,
            _ => 0
        };

        int bonusDamage = duelistLevel;

        int bonusAb = duelistLevel switch
        {
            5 => 1,
            _ => 0
        };

        message = $"Applied Duelist +{bonusAc} shield AC and +{bonusDamage} damage bonus.";
        if (bonusAb != 0)
            message = $"Applied Duelist +{bonusAc} shield AC, +{bonusDamage} damage, and +{bonusAb} attack bonus.";

        if (bonusAb == 0)
            return Effect.LinkEffects
            (
                Effect.ACIncrease(bonusAc, ACBonus.ShieldEnchantment),
                Effect.DamageIncrease(bonusDamage, DamageType.BaseWeapon)
            );

        return Effect.LinkEffects
        (
            Effect.ACIncrease(bonusAc, ACBonus.ShieldEnchantment),
            Effect.DamageIncrease(bonusDamage, DamageType.BaseWeapon),
            Effect.AttackIncrease(bonusAb)
        );
    }
}
