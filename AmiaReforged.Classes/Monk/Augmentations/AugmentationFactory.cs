using AmiaReforged.Classes.Monk.Types;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(AugmentationFactory))]
public class AugmentationFactory
{
    private readonly Dictionary<PathType, IAugmentation>? _augmentations;

    public AugmentationFactory(IEnumerable<IAugmentation> augmentations)
    {
        _augmentations = augmentations.ToDictionary(t => t.PathType);
    }

    public IAugmentation? GetAugmentation(PathType path)
    {
        return _augmentations?.GetValueOrDefault(path);
    }
}

