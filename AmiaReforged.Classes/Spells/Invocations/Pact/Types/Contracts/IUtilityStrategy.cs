using Anvil.API;

namespace AmiaReforged.Classes.Spells.Invocations.Pact.Types.Contracts;

public interface IUtilityStrategy
{
    void Utilize(NwCreature target, NwCreature caster);
}