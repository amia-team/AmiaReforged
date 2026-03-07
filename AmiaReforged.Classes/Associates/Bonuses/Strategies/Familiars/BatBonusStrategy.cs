using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates.Bonuses.Strategies.Familiars;

[ServiceBinding(typeof(IFamiliarBonusStrategy))]
public class BatBonusStrategy : IFamiliarBonusStrategy
{
    public string ResRefPrefix => "nw_fm_bat";

    public void Apply(NwCreature owner, NwCreature associate)
    {
        NwItem? bite = associate.GetItemInSlot(InventorySlot.CreatureBiteWeapon);

        if (!owner.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        if (bite is null)
        {
            return;
        }

        // Wipe existing item properties to prevent stacking with other bonuses and ensure consistent scaling
        bite.RemoveItemProperties();

        int enhancementBonus = 1 + associate.Level / 5;

        bite.AddItemProperty(ItemProperty.EnhancementBonus(enhancementBonus), EffectDuration.Permanent);

        ItemProperty sonicDamage = enhancementBonus switch
        {
            1 => ItemProperty.DamageBonus(IPDamageType.Sonic, IPDamageBonus.Plus1),
            2 => ItemProperty.DamageBonus(IPDamageType.Sonic, IPDamageBonus.Plus2),
            3 => ItemProperty.DamageBonus(IPDamageType.Sonic, IPDamageBonus.Plus3),
            4 => ItemProperty.DamageBonus(IPDamageType.Sonic, IPDamageBonus.Plus4),
            _ => ItemProperty.DamageBonus(IPDamageType.Sonic, IPDamageBonus.Plus5)
        };
        bite.AddItemProperty(sonicDamage, EffectDuration.Permanent);

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
        bite.AddItemProperty(monsterDamage, EffectDuration.Permanent);
    }
}
