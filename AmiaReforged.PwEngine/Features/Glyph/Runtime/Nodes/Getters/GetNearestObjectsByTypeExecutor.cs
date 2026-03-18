using AmiaReforged.PwEngine.Features.Glyph.Core;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns a list of nearby game objects of a specified type, ordered by distance from an origin object.
/// The NWN object type is selected via the <c>type</c> property override (defaults to <c>"Creature"</c>).
/// <para>
/// Supported types: <c>Creature</c>, <c>Placeable</c>, <c>Door</c>, <c>Trigger</c>,
/// <c>AreaOfEffect</c>, <c>Waypoint</c>, <c>Store</c>, <c>Item</c>.
/// </para>
/// <para>
/// The output <c>objects</c> pin produces a <c>List&lt;uint&gt;</c> of object IDs suitable for
/// use with the <see cref="Nodes.Flow.ForEachExecutor"/> node. The <c>count</c> pin gives the
/// number of objects found.
/// </para>
/// </summary>
public class GetNearestObjectsByTypeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.nearest_objects_by_type";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? originVal = await resolveInput("origin");
        uint originId = Convert.ToUInt32(originVal ?? 0);

        object? maxCountVal = await resolveInput("max_count");
        int maxCount = Convert.ToInt32(maxCountVal ?? 10);
        if (maxCount <= 0) maxCount = 10;

        object? tagVal = await resolveInput("tag");
        string tag = tagVal?.ToString() ?? string.Empty;

        string objectType = node.PropertyOverrides.TryGetValue("type", out string? typeOverride)
            ? typeOverride
            : "Creature";

        // Guard: NWN uses 0x7F000000 as OBJECT_INVALID; treat 0 the same way.
        if (originId == 0 || originId == 0x7F000000)
        {
            return EmptyResult();
        }

        NwGameObject? origin = originId.ToNwObject<NwGameObject>();
        if (origin == null)
        {
            return EmptyResult();
        }

        List<uint> objectIds = CollectNearbyObjects(origin, objectType, maxCount, tag);

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["objects"] = objectIds,
            ["count"] = objectIds.Count
        });
    }

    private static GlyphNodeResult EmptyResult() => GlyphNodeResult.Data(new Dictionary<string, object?>
    {
        ["objects"] = new List<uint>(),
        ["count"] = 0
    });

    /// <summary>
    /// Dispatches to the appropriate generic <c>GetNearestObjectsByType&lt;T&gt;</c> call
    /// based on the type name string, optionally filtering by tag.
    /// </summary>
    internal static List<uint> CollectNearbyObjects(NwGameObject origin, string objectType, int maxCount, string tag)
    {
        return objectType switch
        {
            "Creature" => Collect<NwCreature>(origin, maxCount, tag),
            "Placeable" => Collect<NwPlaceable>(origin, maxCount, tag),
            "Door" => Collect<NwDoor>(origin, maxCount, tag),
            "Trigger" => Collect<NwTrigger>(origin, maxCount, tag),
            "AreaOfEffect" => Collect<NwAreaOfEffect>(origin, maxCount, tag),
            "Waypoint" => Collect<NwWaypoint>(origin, maxCount, tag),
            "Store" => Collect<NwStore>(origin, maxCount, tag),
            "Item" => Collect<NwItem>(origin, maxCount, tag),
            _ => []
        };
    }

    private static List<uint> Collect<T>(NwGameObject origin, int maxCount, string tag) where T : NwGameObject
    {
        IEnumerable<T> query = origin.GetNearestObjectsByType<T>();

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(obj => string.Equals(obj.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .Take(maxCount)
            .Select(obj => obj.ObjectId)
            .ToList();
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Nearest Objects By Type",
        Category = "Getters",
        Description = "Returns nearby game objects of a specified type, ordered by distance from the origin. " +
                      "Optionally filter by tag (leave empty for no tag filter).",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        Properties =
        [
            new GlyphPropertyDefinition
            {
                Id = "type",
                DisplayName = "Object Type",
                DefaultValue = "Creature",
                AllowedValues = ["Creature", "Placeable", "Door", "Trigger", "AreaOfEffect", "Waypoint", "Store", "Item"]
            }
        ],
        InputPins =
        [
            new GlyphPin
            {
                Id = "origin", Name = "Origin", DataType = GlyphDataType.NwObject,
                Direction = GlyphPinDirection.Input
            },
            new GlyphPin
            {
                Id = "max_count", Name = "Max Count", DataType = GlyphDataType.Int,
                Direction = GlyphPinDirection.Input, DefaultValue = "10"
            },
            new GlyphPin
            {
                Id = "tag", Name = "Tag", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input, DefaultValue = ""
            }
        ],
        OutputPins =
        [
            new GlyphPin
            {
                Id = "objects", Name = "Objects", DataType = GlyphDataType.List,
                Direction = GlyphPinDirection.Output
            },
            new GlyphPin
            {
                Id = "count", Name = "Count", DataType = GlyphDataType.Int,
                Direction = GlyphPinDirection.Output
            }
        ]
    };
}
