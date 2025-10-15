using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public static class MonkPathMap
{
    public static readonly Dictionary<PathType, (string PathName, string PathAbilities, string PathIcon)> PathMap = new()
    {
        { PathType.CrashingMeteor, ("Crashing Meteor", MonkPathNuiElements.CrashingMeteorAbilities, MonkPathNuiElements.CrashingMeteorIcon)},
        { PathType.EchoingValley, ("Echoing Valley", MonkPathNuiElements.EchoingValleyAbilities, MonkPathNuiElements.EchoingValleyIcon)},
        { PathType.FickleStrand, ("Fickle Strand", MonkPathNuiElements.FickleStrandAbilities, MonkPathNuiElements.FickleStrandIcon)},
        { PathType.FloatingLeaf, ("Floating Leaf", MonkPathNuiElements.FloatingLeafAbilities, MonkPathNuiElements.FloatingLeafIcon)},
        { PathType.IroncladBull, ("Ironclad Bull", MonkPathNuiElements.IroncladBullAbilities, MonkPathNuiElements.IroncladBullIcon)},
        { PathType.SplinteredChalice, ("Splintered Chalice", MonkPathNuiElements.SplinteredChaliceAbilities,MonkPathNuiElements.SplinteredChaliceIcon)},
        { PathType.SwingingCenser, ("Swinging Censer", MonkPathNuiElements.SwingingCenserAbilities, MonkPathNuiElements.SwingingCenserIcon)}
    };

    public static readonly Dictionary<PathType, NwFeat?> PathToFeat = new()
    {
        { PathType.CrashingMeteor, NwFeat.FromFeatId(MonkFeat.PoeCrashingMeteor) },
        { PathType.EchoingValley, NwFeat.FromFeatId(MonkFeat.PoeEchoingValley) },
        { PathType.FickleStrand, NwFeat.FromFeatId(MonkFeat.PoeFickleStrand) },
        { PathType.FloatingLeaf, NwFeat.FromFeatId(MonkFeat.PoeFloatingLeaf) },
        { PathType.IroncladBull, NwFeat.FromFeatId(MonkFeat.PoeIroncladBull) },
        { PathType.SplinteredChalice, NwFeat.FromFeatId(MonkFeat.PoeSplinteredChalice) },
        { PathType.SwingingCenser, NwFeat.FromFeatId(MonkFeat.PoeSwingingCenser) }
    };
}
