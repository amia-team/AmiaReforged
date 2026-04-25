using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons;

[ServiceBinding(typeof(IBoon))]
public class BoonOfMagicalDefense : IBoon
{
    public BoonType BoonType => BoonType.MagicalDefense;

    public int GetBoonAmount(int bloodswornLevel) => 22 + bloodswornLevel * 2;

    public Effect GetBoonEffect(int bloodswornLevel)
        => Effect.LinkEffects(
            Effect.SpellResistanceIncrease(GetBoonAmount(bloodswornLevel)),
                Effect.SavingThrowIncrease(SavingThrow.All, 2, SavingThrowType.Spell));

    public string GetBoonMessage(int bloodswornLevel)
        => $"Boon of Magical Defense: +{GetBoonAmount(bloodswornLevel)} Spell Resistance and +2 Saving Throws vs. Spells";
}
