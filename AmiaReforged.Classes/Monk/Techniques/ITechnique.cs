using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques;

public interface ITechnique
{
    TechniqueType TechniqueType { get; }
    void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData);
    void HandleDamageTechnique(NwCreature monk, OnCreatureDamage damageData);
    void HandleCastTechnique(NwCreature monk, OnSpellCast castData);
}
