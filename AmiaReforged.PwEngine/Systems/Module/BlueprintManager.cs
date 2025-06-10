using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Module;

[ServiceBinding(typeof(BlueprintManager))]
public sealed class BlueprintManager
{
    private readonly List<IBlueprintSource> _blueprintSources;

    public BlueprintManager(IEnumerable<IBlueprintSource> blueprintSources)
    {
        this._blueprintSources = blueprintSources.ToList();
    }

    public List<IBlueprint> GetMatchingBlueprints(BlueprintObjectType objectType, string search, int max)
    {
        List<IBlueprint> results = new();
        // First, try to get equal results from each source
        int each = max / _blueprintSources.Count;

        foreach (IBlueprintSource blueprintSource in _blueprintSources)
        {
            results.AddRange(blueprintSource.GetBlueprints(objectType, 0, search, each));
        }

        if (results.Count < max)
        {
            foreach (IBlueprintSource blueprintSource in _blueprintSources)
            {
                results.AddRange(blueprintSource.GetBlueprints(objectType, each, search, max - results.Count));
            }
        }

        results.Sort((blueprintA, blueprintB) =>
            string.Compare(blueprintA.FullName, blueprintB.FullName, StringComparison.OrdinalIgnoreCase));
        return results;
    }
}