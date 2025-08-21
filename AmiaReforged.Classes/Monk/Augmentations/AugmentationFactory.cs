using AmiaReforged.Classes.Monk.Techniques;
using AmiaReforged.Classes.Monk.Types;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(AugmentationFactory))]
public class AugmentationFactory
{
    private static Dictionary<PathType, IAugmentation>? _augmentations;

    public AugmentationFactory(List<IAugmentation> augmentations)
    {
        _augmentations = augmentations.ToDictionary(t => t.PathType);
    }

    public static IAugmentation? GetAugmentation(PathType path)
    {
        return _augmentations?.GetValueOrDefault(path);
    }
}

