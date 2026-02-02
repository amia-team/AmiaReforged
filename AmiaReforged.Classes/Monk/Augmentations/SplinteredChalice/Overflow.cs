using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

public static class Overflow
{
    public const string EffectTag = nameof(PathType.SplinteredChalice) + "Overflow";
    public static bool HasOverflow(NwCreature monk) => monk.ActiveEffects.Any(e => e.Tag == EffectTag);
}
