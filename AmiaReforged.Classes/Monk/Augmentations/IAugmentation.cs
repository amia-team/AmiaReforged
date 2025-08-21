using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public interface IAugmentation
{
    PathType PathType { get; }
    void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureAttack attackData);
    void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData);
}
