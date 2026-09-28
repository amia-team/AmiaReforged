using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Shared type-dispatch for Glyph object-query getters over NWN game objects.
/// A single case-insensitive mapping backs both the curated singular
/// <see cref="GetNearestObjectByTypeExecutor"/> and the existing list
/// <see cref="GetNearestObjectsByTypeExecutor"/>, so the object-type switch is
/// maintained in one place rather than duplicated across executors.
/// <para>
/// All type names are matched case-insensitively; the canonical <c>Object.*</c>
/// spellings are lowercase.
/// </para>
/// </summary>
internal static class NwObjectQuery
{
    /// <summary>
    /// Canonical lowercase type spellings exposed by the curated <c>Object.*</c> surface.
    /// </summary>
    public static readonly HashSet<string> CuratedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "trigger", "door", "placeable", "creature", "waypoint"
    };

    /// <summary>
    /// Returns true when <paramref name="type"/> is one of the five curated types
    /// (case-insensitive). Null/empty is not a curated type.
    /// </summary>
    public static bool IsCuratedType(string? type) =>
        !string.IsNullOrEmpty(type) && CuratedTypes.Contains(type);

    /// <summary>
    /// Returns the nearest object id of the requested curated type, ordered by distance
    /// from <paramref name="origin"/>, or <see cref="NWScript.OBJECT_INVALID"/> when the
    /// type is not part of the curated set.
    /// </summary>
    public static uint NearestObjectId(NwGameObject origin, string? type, string tag = "")
    {
        if (!IsCuratedType(type))
            return NWScript.OBJECT_INVALID;

        string key = type!.Trim().ToLowerInvariant();
        uint? nearest = key switch
        {
            "trigger"   => FirstOf<NwTrigger>(origin, tag),
            "door"      => FirstOf<NwDoor>(origin, tag),
            "placeable" => FirstOf<NwPlaceable>(origin, tag),
            "creature"  => FirstOf<NwCreature>(origin, tag),
            "waypoint"  => FirstOf<NwWaypoint>(origin, tag),
            _ => null
        };
        return nearest ?? NWScript.OBJECT_INVALID;
    }

    /// <summary>
    /// Returns up to <paramref name="maxCount"/> object ids of the requested type, ordered
    /// by distance. Accepts the broader set used by the list executor (case-insensitive)
    /// and returns an empty list for unknown types.
    /// </summary>
    public static List<uint> Collect(NwGameObject origin, string? type, int maxCount, string tag = "")
    {
        if (maxCount <= 0) maxCount = 10;

        string key = type?.Trim().ToLowerInvariant() ?? string.Empty;
        switch (key)
        {
            case "trigger":    return CollectOf<NwTrigger>(origin, maxCount, tag);
            case "door":       return CollectOf<NwDoor>(origin, maxCount, tag);
            case "placeable":  return CollectOf<NwPlaceable>(origin, maxCount, tag);
            case "creature":   return CollectOf<NwCreature>(origin, maxCount, tag);
            case "waypoint":   return CollectOf<NwWaypoint>(origin, maxCount, tag);
            case "areaofeffect": return CollectOf<NwAreaOfEffect>(origin, maxCount, tag);
            case "store":      return CollectOf<NwStore>(origin, maxCount, tag);
            case "item":       return CollectOf<NwItem>(origin, maxCount, tag);
            default:           return [];
        }
    }

    private static List<uint> CollectOf<T>(NwGameObject origin, int maxCount, string tag) where T : NwGameObject
    {
        IEnumerable<T> query = origin.GetNearestObjectsByType<T>();
        if (!string.IsNullOrEmpty(tag))
            query = query.Where(obj => string.Equals(obj.Tag, tag, StringComparison.OrdinalIgnoreCase));
        return query.Take(maxCount).Select(obj => obj.ObjectId).ToList();
    }

    private static uint? FirstOf<T>(NwGameObject origin, string tag) where T : NwGameObject
    {
        IEnumerable<T> query = origin.GetNearestObjectsByType<T>();
        if (!string.IsNullOrEmpty(tag))
            query = query.Where(obj => string.Equals(obj.Tag, tag, StringComparison.OrdinalIgnoreCase));
        return query.Select(obj => obj.ObjectId).FirstOrDefault();
    }
}
