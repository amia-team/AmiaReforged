using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

[ServiceBinding(typeof(IItemDefinitionRepository))]
public class InMemoryItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly Dictionary<string, ItemData.ItemBlueprint> _itemDefinitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ItemData.ItemBlueprint> _bySourceFile = new(StringComparer.OrdinalIgnoreCase);

    public void AddItemDefinition(ItemData.ItemBlueprint definition)
    {
        bool added = _itemDefinitions.TryAdd(definition.ItemTag, definition);

        if (!added)
        {
            _itemDefinitions[definition.ItemTag] = definition;
        }

        // Also index by source filename for fallback lookup
        if (!string.IsNullOrWhiteSpace(definition.SourceFile))
        {
            _bySourceFile[definition.SourceFile] = definition;
        }
    }

    public ItemData.ItemBlueprint? GetByTag(string harvestOutputItemDefinitionTag)
    {
        // First try exact tag match
        if (_itemDefinitions.TryGetValue(harvestOutputItemDefinitionTag, out var blueprint))
        {
            return blueprint;
        }

        // Fallback: try matching by source filename (for when ItemTag doesn't match filename)
        if (_bySourceFile.TryGetValue(harvestOutputItemDefinitionTag, out blueprint))
        {
            return blueprint;
        }

        return null;
    }

    public ItemData.ItemBlueprint? GetByResRef(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef)) return null;
        return _itemDefinitions.Values.FirstOrDefault(d => string.Equals(d.ResRef, resRef, StringComparison.OrdinalIgnoreCase));
    }

    public List<ItemData.ItemBlueprint> AllItems()
    {
        return _itemDefinitions.Values.ToList();
    }

    public List<string> FindSimilarTags(string tag, int maxResults = 3)
    {
        if (string.IsNullOrWhiteSpace(tag)) return new List<string>();
        
        // Search both ItemTags and SourceFile names for suggestions
        var allKeys = _itemDefinitions.Keys
            .Concat(_bySourceFile.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return allKeys
            .Select(existingTag => new { Tag = existingTag, Score = CalculateSimilarity(tag, existingTag) })
            .Where(x => x.Score > 0.3) // Threshold for "similar enough"
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.Tag)
            .ToList();
    }

    private static double CalculateSimilarity(string source, string target)
    {
        source = source.ToLowerInvariant();
        target = target.ToLowerInvariant();

        // Check for substring match (high score)
        if (target.Contains(source) || source.Contains(target))
        {
            return 0.8;
        }

        // Levenshtein distance-based similarity
        int distance = LevenshteinDistance(source, target);
        int maxLen = Math.Max(source.Length, target.Length);
        if (maxLen == 0) return 1.0;
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        int[,] dp = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= target.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[source.Length, target.Length];
    }
}
