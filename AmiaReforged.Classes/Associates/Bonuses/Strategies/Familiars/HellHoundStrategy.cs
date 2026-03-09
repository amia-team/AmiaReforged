using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Associates.Bonuses.Strategies.Familiars;

[ServiceBinding(typeof(IFamiliarBonusStrategy))]
public class HellHoundStrategy : IFamiliarBonusStrategy
{
    public string ResRefPrefix => "nw_fm_hell";

    public void Apply(NwCreature owner, NwCreature associate)
    {
        // n.b.: Hell hounds already have their base AC manually incremented in the toolset. Step is skipped.

        NwItem? bite = associate.GetItemInSlot(InventorySlot.CreatureBiteWeapon);

        bite?.RemoveItemProperties();

            int enhancementBonus = 1 + associate.Level / 5;

            bite?.AddItemProperty(ItemProperty.EnhancementBonus(enhancementBonus), EffectDuration.Permanent);

            ItemProperty fireDamage = enhancementBonus switch
            {
                1 => ItemProperty.DamageBonus(IPDamageType.Fire, IPDamageBonus.Plus1),
                2 => ItemProperty.DamageBonus(IPDamageType.Fire, IPDamageBonus.Plus2),
                3 => ItemProperty.DamageBonus(IPDamageType.Fire, IPDamageBonus.Plus3),
                4 => ItemProperty.DamageBonus(IPDamageType.Fire, IPDamageBonus.Plus4),
                _ => ItemProperty.DamageBonus(IPDamageType.Fire, IPDamageBonus.Plus5)
            };
            bite?.AddItemProperty(fireDamage, EffectDuration.Permanent);

            ItemProperty monsterDamage = associate.Level switch
            {
                >= 10 and < 20 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d10),
                >= 20 and < 29 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d12),
                >= 30 and < 35 => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d20),
                _ => ItemProperty.MonsterDamage(IPMonsterDamage.Damage1d8)
            };
            bite?.AddItemProperty(monsterDamage, EffectDuration.Permanent);
    }
}
