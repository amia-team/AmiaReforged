using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public interface IAugmentation
{
    PathType Path { get; }
    TechniqueType Technique { get; }

    public interface IAttackAugment : IAugmentation
    {
        void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique);
    }

    public interface ICastAugment : IAugmentation
    {
        void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique);
    }
}
