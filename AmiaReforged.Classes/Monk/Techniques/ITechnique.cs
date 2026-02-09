using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques;

public interface ITechnique
{
    TechniqueType Technique { get; }
}

public interface ICastTechnique : ITechnique
{
    void HandleCastTechnique(NwCreature monk, OnSpellCast castData);
}

public interface IAttackTechnique : ITechnique
{
    void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData);
}
