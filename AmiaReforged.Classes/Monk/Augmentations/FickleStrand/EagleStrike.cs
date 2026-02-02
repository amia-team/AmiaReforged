using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.Classes.Monk.WildMagic;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FickleStrand;

[ServiceBinding(typeof(IAugmentation))]
public class EagleStrike(WildMagicService wildMagicService) : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.FickleStrand;
    public TechniqueType Technique => TechniqueType.EagleStrike;
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentEagleStrike(monk, damageData);
    }

    /// <summary>
    /// Eagle Strike has a 30% chance to impart a wild magic effect.
    /// Each Ki Focus makes potent effects more likely to occur.
    /// </summary>
    private void AugmentEagleStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        if (damageData.Target is not NwCreature targetCreature || !monk.IsReactionTypeHostile(targetCreature)) return;

        if (Random.Shared.Roll(100) <= 30)
            wildMagicService.DoWildMagic(monk, targetCreature);
    }
}
