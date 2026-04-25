using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Bloodsworn.Boons;

[ServiceBinding(typeof(IBoon))]
public class BoonOfLife : IBoon
{
    public BoonType BoonType => BoonType.Life;

    public int GetBoonAmount(int bloodswornLevel) => bloodswornLevel switch
    {
        >= 5 => 3,
        >= 3 => 2,
        _ => 1
    };

    public Effect GetBoonEffect(int bloodswornLevel) =>
        Effect.Regenerate(GetBoonAmount(bloodswornLevel), interval: TimeSpan.FromSeconds(6));

    public string GetBoonMessage(int bloodswornLevel)
        => $"Boon of Life: +{GetBoonAmount(bloodswornLevel)} Regeneration";
}
