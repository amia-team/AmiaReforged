using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public interface IAugmentation
{
    PathType PathType { get; }
    void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData);
    void ApplyDamageAugmentation(NwCreature monk, TechniqueType technique, OnCreatureDamage damageData);
    void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData);
}
