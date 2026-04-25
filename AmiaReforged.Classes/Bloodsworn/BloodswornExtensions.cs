using NWN.Core;
using Anvil.API;

namespace AmiaReforged.Classes.Bloodsworn;

public static class BloodswornExtensions
{
    public const int BloodswornId = 56;
    public static int BloodswornLevel(this NwCreature creature) => NWScript.GetLevelByClass(BloodswornId, creature);
}
