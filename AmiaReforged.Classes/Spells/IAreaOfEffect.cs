using Anvil.API;

namespace AmiaReforged.Classes.Spells;

public interface IAreaOfEffect
{
    void TriggerOnEnter(NwCreature? enteringObject);
    void TriggerHeartbeat(NwCreature lingeringObject);
    void TriggerOnExit(NwCreature exitingObject);
}