using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates.Bonuses.Strategies.Familiars;

[ServiceBinding(typeof(IFamiliarBonusStrategy))]
public class RavenBonusStrategy : IFamiliarBonusStrategy
{
    public string ResRefPrefix => "nw_fm_rave";

    public void Apply(NwCreature owner, NwCreature associate)
    {
        int baseAc = 10 + associate.Level / 2;
        associate.BaseAC = (sbyte)baseAc;

        NwItem? claw1 = associate.GetItemInSlot(InventorySlot.CreatureLeftWeapon);
        NwItem? claw2 = associate.GetItemInSlot(InventorySlot.CreatureRightWeapon);


        if(!owner.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        if(claw1 is null || claw2 is null)
        {
            return;
        }

        claw1.RemoveItemProperties();
        claw2.RemoveItemProperties();

        int enhancementBonus = 1 + associate.Level / 5;
        claw1.AddItemProperty(ItemProperty.EnhancementBonus(enhancementBonus), EffectDuration.Permanent);
        claw2.AddItemProperty(ItemProperty.EnhancementBonus(enhancementBonus), EffectDuration.Permanent);

        ItemProperty damageBonus = enhancementBonus switch
        {
            1 => ItemProperty.DamageBonus(IPDamageType.Piercing, IPDamageBonus.Plus1),
            2 => ItemProperty.DamageBonus(IPDamageType.Piercing, IPDamageBonus.Plus2),
            3 => ItemProperty.DamageBonus(IPDamageType.Piercing, IPDamageBonus.Plus3),
            4 => ItemProperty.DamageBonus(IPDamageType.Piercing, IPDamageBonus.Plus4),
            _ => ItemProperty.DamageBonus(IPDamageType.Piercing, IPDamageBonus.Plus5)
        };
        claw1.AddItemProperty(damageBonus, EffectDuration.Permanent);
        claw2.AddItemProperty(damageBonus, EffectDuration.Permanent);


        ItemProperty monsterDamage = associate.Level switch
        {
            >= 5 and < 10 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d3),
            >= 10 and < 15 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d4),
            >= 15 and < 20 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d6),
            >= 20 and < 25 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d8),
            >= 25 and < 30 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d10),
            >= 30 and < 35 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d12),
            _ => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d2)
        };
        claw1.AddItemProperty(monsterDamage, EffectDuration.Permanent);
        claw2.AddItemProperty(monsterDamage, EffectDuration.Permanent);

        ItemProperty blind = associate.Level switch
        {
            >= 10 and < 20 => ItemProperty.OnHitEffect(IPOnHitSaveDC.DC16,
                HitEffect.Blindness(IPOnHitDuration.Duration75Pct1Round)),
            >= 20 and < 30 => ItemProperty.OnHitEffect(IPOnHitSaveDC.DC20,
                HitEffect.Blindness(IPOnHitDuration.Duration75Pct1Round)),
            >= 30 => ItemProperty.OnHitEffect(IPOnHitSaveDC.DC26,
                HitEffect.Blindness(IPOnHitDuration.Duration75Pct1Round)),
            _ => ItemProperty.OnHitEffect(IPOnHitSaveDC.DC14, HitEffect.Blindness(IPOnHitDuration.Duration75Pct1Round))
        };
        claw1.AddItemProperty(blind, EffectDuration.Permanent);
        claw2.AddItemProperty(blind, EffectDuration.Permanent);
    }
}
