using AmiaReforged.Classes.Bloodsworn.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Bloodsworn.Boons;

public interface IBoon
{
    BoonType BoonType { get; }

    int GetBoonAmount(int bloodswornLevel);

    Effect GetBoonEffect(int bloodswornLevel);

    string GetBoonMessage(int bloodswornLevel);
}
