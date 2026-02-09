using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.Classes.Monk.WildMagic;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FickleStrand;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentEagleStrike(WildMagicService wildMagicService) : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.FickleStrand;
    public TechniqueType Technique => TechniqueType.EagleStrike;

    /// <summary>
    /// 30% chance to trigger a Wild Magic effect. Ki Focus increases the potency of effects (Weak to Strong).
    /// </summary>
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        if (damageData.Target is not NwCreature targetCreature || !monk.IsReactionTypeHostile(targetCreature)) return;

        if (Random.Shared.Roll(100) <= 30)
            wildMagicService.DoWildMagic(monk, targetCreature);
    }
}
