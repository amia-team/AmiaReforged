using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(IAugmentation))]
public class QuiveringPalm : IAugmentation.ICastAugment
{
    public PathType Path => PathType.SplinteredChalice;
    public TechniqueType Technique => TechniqueType.QuiveringPalm;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        AugmentQuiveringPalm(monk, castData);
    }

    /// <summary>
    /// Quivering Palm deals negative damage with a 25% multiplier, with extra 25% per Ki Focus.
    /// Overflow: Deals divine damage instead, and the monk is healed for 50% of their missing hit points.
    /// </summary>
    private void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData)
    {
        throw new NotImplementedException();
    }
}
