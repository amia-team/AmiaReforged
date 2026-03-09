namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;

/// <summary>
/// In-memory implementation of <see cref="IInteractionDefinitionRepository"/> for testing.
/// </summary>
internal class InMemoryInteractionDefinitionRepository : IInteractionDefinitionRepository
{
    private readonly Dictionary<string, InteractionDefinition> _definitions = new(StringComparer.OrdinalIgnoreCase);

    public InteractionDefinition? Get(string tag)
        => _definitions.GetValueOrDefault(tag);

    public List<InteractionDefinition> All()
        => _definitions.Values.OrderBy(d => d.Name).ToList();

    public bool Exists(string tag)
        => _definitions.ContainsKey(tag);

    public void Create(InteractionDefinition definition)
        => _definitions[definition.Tag] = definition;

    public void Update(InteractionDefinition definition)
    {
        if (_definitions.ContainsKey(definition.Tag))
            _definitions[definition.Tag] = definition;
    }

    public bool Delete(string tag)
        => _definitions.Remove(tag);

    public List<InteractionDefinition> Search(string? search, int page, int pageSize, out int totalCount)
    {
        IEnumerable<InteractionDefinition> query = _definitions.Values;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.Tag.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        List<InteractionDefinition> results = query.OrderBy(d => d.Name).ToList();
        totalCount = results.Count;

        return results
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }
}
