using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(IAugmentation))]
public class EagleStrike : IAugmentation.IDamageAugment
{
    public PathType Path => PathType.SplinteredChalice;
    public TechniqueType Technique => TechniqueType.EagleStrike;
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        AugmentEagleStrike(monk, damageData);
    }

    /// <summary>
    /// Eagle Strike inflicts 3% physical and negative vulnerability, with extra 3% per Ki Focus.
    /// Overflow: Inflicts 5% physical and divine vulnerability, with extra 5% per Ki Focus.
    /// </summary>
    private void AugmentEagleStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        throw new NotImplementedException();
    }
}
