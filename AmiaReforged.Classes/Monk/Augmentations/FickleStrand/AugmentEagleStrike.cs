using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.Classes.Monk.WildMagic;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FickleStrand;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentEagleStrike(WildMagicService wildMagicService) : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.FickleStrand;
    public TechniqueType Technique => TechniqueType.EagleStrike;

    /// <summary>
    /// 30% chance to trigger a Wild Magic effect. Ki Focus increases the potency of effects (Weak to Strong).
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        if (attackData.Target is not NwCreature targetCreature || !monk.IsReactionTypeHostile(targetCreature)) return;

        if (Random.Shared.Roll(100) <= 30)
            wildMagicService.DoWildMagic(monk, targetCreature);
    }
}
