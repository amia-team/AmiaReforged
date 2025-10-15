using AmiaReforged.Classes.Monk.Types;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques;

[ServiceBinding(typeof(TechniqueFactory))]
public class TechniqueFactory
{
    private readonly Dictionary<TechniqueType, ITechnique>? _techniques;

    public TechniqueFactory(IEnumerable<ITechnique> techniques)
    {
        _techniques = techniques.ToDictionary(t => t.TechniqueType);
    }

    public ITechnique? GetTechnique(TechniqueType techniqueType)
    {
        return _techniques?.GetValueOrDefault(techniqueType);
    }
}
