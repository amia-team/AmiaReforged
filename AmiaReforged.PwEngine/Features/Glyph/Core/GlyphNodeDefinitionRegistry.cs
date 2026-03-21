using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// In-memory registry of all <see cref="GlyphNodeDefinition"/>s. Populated at startup
/// by node registrar services. Thread-safe for reads after initialization.
/// </summary>
[ServiceBinding(typeof(IGlyphNodeDefinitionRegistry))]
public class GlyphNodeDefinitionRegistry : IGlyphNodeDefinitionRegistry
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<string, GlyphNodeDefinition> _definitions = new(StringComparer.Ordinal);
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Register(GlyphNodeDefinition definition)
    {
        if (definition == null) throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(definition.TypeId))
            throw new ArgumentException("TypeId must not be empty.", nameof(definition));

        lock (_lock)
        {
            if (_definitions.ContainsKey(definition.TypeId))
            {
                throw new InvalidOperationException(
                    $"A Glyph node definition with TypeId '{definition.TypeId}' is already registered.");
            }

            _definitions[definition.TypeId] = definition;
            Log.Info("Registered Glyph node definition: {TypeId} ({DisplayName})",
                definition.TypeId, definition.DisplayName);
        }
    }

    /// <inheritdoc />
    public GlyphNodeDefinition? Get(string typeId)
    {
        lock (_lock)
        {
            return _definitions.GetValueOrDefault(typeId);
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<GlyphNodeDefinition> GetAll()
    {
        lock (_lock)
        {
            return _definitions.Values.ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<GlyphNodeDefinition> GetForEventType(GlyphEventType eventType)
    {
        lock (_lock)
        {
            return _definitions.Values
                .Where(d => d.RestrictToEventType == null || d.RestrictToEventType == eventType)
                .ToList();
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetCategories()
    {
        lock (_lock)
        {
            return _definitions.Values
                .Select(d => d.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}
