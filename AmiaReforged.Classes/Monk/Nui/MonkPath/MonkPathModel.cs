using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using static AmiaReforged.Classes.Monk.Nui.MonkPath.MonkPathNuiElements;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public class MonkPathModel
{
    public record MonkPathData
    (
        PathType Type,
        string Name,
        string Description,
        string Abilities,
        string Icon,
        int FeatId
    );

    public readonly List<MonkPathData> Paths =
    [
        new(PathType.CrashingMeteor,
            "Crashing Meteor",
            CrashingMeteorDescription,
            CrashingMeteorAbilities,
            CrashingMeteorIcon,
            MonkFeat.PoeCrashingMeteor),


        new(PathType.EchoingValley,
            "Echoing Valley",
            EchoingValleyDescription,
            EchoingValleyAbilities,
            EchoingValleyIcon,
            MonkFeat.PoeEchoingValley),


        new(PathType.FickleStrand,
            "Fickle Strand",
            FickleStrandDescription,
            FickleStrandAbilities,
            FickleStrandIcon,
            MonkFeat.PoeFickleStrand),


        new(PathType.FloatingLeaf,
            "Floating Leaf",
            FloatingLeafDescription,
            FloatingLeafAbilities,
            FloatingLeafIcon,
            MonkFeat.PoeFloatingLeaf),


        new(PathType.IroncladBull,
            "Ironclad Bull",
            IroncladBullDescription,
            IroncladBullAbilities,
            IroncladBullIcon,
            MonkFeat.PoeIroncladBull),


        new(PathType.SplinteredChalice,
            "Splintered Chalice",
            SplinteredChaliceDescription,
            SplinteredChaliceAbilities,
            SplinteredChaliceIcon,
            MonkFeat.PoeSplinteredChalice),


        new(PathType.SwingingCenser,
            "Swinging Censer",
            SwingingCenserDescription,
            SwingingCenserAbilities,
            SwingingCenserIcon,
            MonkFeat.PoeSwingingCenser)
    ];

    public MonkPathData? Get(PathType type) => Paths.Find(p => p.Type == type);
}
