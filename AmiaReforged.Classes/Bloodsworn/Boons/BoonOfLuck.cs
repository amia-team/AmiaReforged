using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons;

[ServiceBinding(typeof(IBoon))]
public class BoonOfLuck : IBoon
{
    public BoonType BoonType => BoonType.Luck;

    public int GetBoonAmount(int bloodswornLevel) => bloodswornLevel switch
    {
        >= 5 => 3,
        >= 3 => 2,
        _ => 1
    };

    public Effect GetBoonEffect(int bloodswornLevel) =>
        Effect.SavingThrowIncrease(SavingThrow.All, GetBoonAmount(bloodswornLevel));

    public string GetBoonMessage(int bloodswornLevel)
        => $"Boon of Luck: +{GetBoonAmount(bloodswornLevel)} Universal Saving Throws";
}
