using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Value object that represents a node tag pattern for harvest effects.
/// Supports three matching modes:
/// <list type="bullet">
///   <item><description>Exact match: <c>"ore_vein_cassiterite"</c></description></item>
///   <item><description>Wildcard glob: <c>"ore_vein_*"</c> or <c>"ore_*_copper"</c> — uses <c>*</c> as "match any characters"</description></item>
///   <item><description>Type prefix: <c>"type:ore"</c> — matches any node whose <see cref="ResourceType"/> equals the suffix (case-insensitive)</description></item>
/// </list>
/// </summary>
public readonly record struct NodeTagPattern
{
    private const string TypePrefix = "type:";

    /// <summary>
    /// The raw pattern string (e.g. <c>"ore_vein_*"</c>, <c>"type:ore"</c>, or <c>"ore_vein_cassiterite"</c>).
    /// </summary>
    public string Pattern { get; }

    /// <summary>
    /// True when the pattern contains a wildcard (<c>*</c>) or uses the <c>type:</c> prefix.
    /// </summary>
    public bool IsWildcard { get; }

    /// <summary>
    /// True when the pattern targets a <see cref="ResourceType"/> via the <c>type:</c> prefix.
    /// </summary>
    public bool IsTypePattern { get; }

    // Lazily compiled regex for glob patterns; null for exact-match and type patterns.
    private readonly Regex? _globRegex;

    // Parsed resource type for type: patterns; null otherwise.
    private readonly ResourceType? _targetType;

    public NodeTagPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Node tag pattern cannot be null or empty.", nameof(pattern));
        }

        Pattern = pattern;

        if (pattern.StartsWith(TypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            IsTypePattern = true;
            IsWildcard = true;

            string typeName = pattern[TypePrefix.Length..];
            if (!Enum.TryParse(typeName, ignoreCase: true, out ResourceType parsedType))
            {
                throw new ArgumentException(
                    $"Unknown resource type '{typeName}'. Valid types: {string.Join(", ", Enum.GetNames<ResourceType>())}",
                    nameof(pattern));
            }

            _targetType = parsedType;
            _globRegex = null;
        }
        else if (pattern.Contains('*'))
        {
            IsWildcard = true;
            IsTypePattern = false;
            _targetType = null;

            // Convert glob pattern to regex: escape everything then replace escaped \* with .*
            string escaped = Regex.Escape(pattern).Replace(@"\*", ".*");
            _globRegex = new Regex($"^{escaped}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        else
        {
            IsWildcard = false;
            IsTypePattern = false;
            _targetType = null;
            _globRegex = null;
        }
    }

    /// <summary>
    /// Tests whether this pattern matches a concrete resource node.
    /// </summary>
    /// <param name="definitionTag">The <see cref="ResourceNodeDefinition.Tag"/> of the node being tested.</param>
    /// <param name="resourceType">The <see cref="ResourceType"/> of the node being tested.</param>
    /// <returns><c>true</c> if the pattern matches; <c>false</c> otherwise.</returns>
    public bool Matches(string definitionTag, ResourceType resourceType)
    {
        if (IsTypePattern)
        {
            return _targetType == resourceType;
        }

        if (_globRegex != null)
        {
            return _globRegex.IsMatch(definitionTag);
        }

        return string.Equals(Pattern, definitionTag, StringComparison.Ordinal);
    }

    /// <summary>
    /// Implicit conversion from <see cref="string"/> so that existing code and JSON deserialization
    /// continue to work without changes.
    /// </summary>
    public static implicit operator NodeTagPattern(string pattern) => new(pattern);

    /// <summary>
    /// Implicit conversion to <see cref="string"/> for serialization and display.
    /// </summary>
    public static implicit operator string(NodeTagPattern pattern) => pattern.Pattern;

    public override string ToString() => Pattern;
}
