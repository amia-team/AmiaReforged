using AmiaReforged.Classes.Monk.Types;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(AugmentationFactory))]
public class AugmentationFactory
{
    private readonly Dictionary<(PathType Path, TechniqueType Technique), IAugmentation> _augments;

    public AugmentationFactory(IEnumerable<IAugmentation> augmentations)
    {
        // Index by both Path and Technique to ensure uniqueness
        _augments = augmentations.ToDictionary(a => (a.Path, a.Technique));
    }

    public IAugmentation? GetAugmentation(PathType path, TechniqueType technique)
    {
        return _augments.GetValueOrDefault((path, technique));
    }
}

