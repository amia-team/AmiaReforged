using AmiaReforged.Classes.Spells.Invocations.Pact.Types.Contracts;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

public sealed class NoMoveStrategy : IMobilityStrategy
{
    public void Move(NwCreature caster, Location location)
    {
        caster.Location = location;
        NWScript.SendMessageToAllDMs(
            $"Bug report: {caster.Name} moved using a mobility invocation that had no strategy assigned to it");
    }
}