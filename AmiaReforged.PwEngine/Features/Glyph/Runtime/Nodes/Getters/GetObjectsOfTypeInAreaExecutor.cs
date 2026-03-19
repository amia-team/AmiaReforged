using AmiaReforged.PwEngine.Features.Glyph.Core;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns all game objects of a specified type within the same area as the reference creature.
/// The NWN object type is selected via the <c>type</c> property override (defaults to <c>"Creature"</c>).
/// <para>
/// Supported types: <c>Creature</c>, <c>Placeable</c>, <c>Door</c>, <c>Trigger</c>,
/// <c>AreaOfEffect</c>, <c>Waypoint</c>, <c>Store</c>, <c>Item</c>.
/// </para>
/// <para>
/// The output <c>objects</c> pin produces a <c>List&lt;uint&gt;</c> of object IDs suitable for
/// use with the <see cref="Nodes.Flow.ForEachExecutor"/> node.
/// </para>
/// </summary>
public class GetObjectsOfTypeInAreaExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.objects_of_type_in_area";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureVal = await resolveInput("creature");
        uint creatureId = Convert.ToUInt32(creatureVal ?? 0);

        object? tagVal = await resolveInput("tag");
        string tag = tagVal?.ToString() ?? string.Empty;

        string objectType = node.PropertyOverrides.TryGetValue("type", out string? typeOverride)
            ? typeOverride
            : "Creature";

        if (creatureId == 0 || creatureId == 0x7F000000)
        {
            return EmptyResult();
        }

        NwGameObject? creature = creatureId.ToNwObject<NwGameObject>();
        NwArea? area = creature?.Area;
        if (area == null)
        {
            return EmptyResult();
        }

        List<uint> objectIds = CollectObjectsInArea(area, objectType, tag);

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

    internal static List<uint> CollectObjectsInArea(NwArea area, string objectType, string tag)
    {
        return objectType switch
        {
            "Creature" => Collect<NwCreature>(area, tag),
            "Placeable" => Collect<NwPlaceable>(area, tag),
            "Door" => Collect<NwDoor>(area, tag),
            "Trigger" => Collect<NwTrigger>(area, tag),
            "AreaOfEffect" => Collect<NwAreaOfEffect>(area, tag),
            "Waypoint" => Collect<NwWaypoint>(area, tag),
            "Store" => Collect<NwStore>(area, tag),
            "Item" => Collect<NwItem>(area, tag),
            _ => []
        };
    }

    private static List<uint> Collect<T>(NwArea area, string tag) where T : NwGameObject
    {
        IEnumerable<T> query = area.Objects.OfType<T>();

        if (!string.IsNullOrEmpty(tag))
        {
            query = query.Where(obj => string.Equals(obj.Tag, tag, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .Select(obj => obj.ObjectId)
            .ToList();
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Objects Of Type In Area",
        Category = "Getters",
        Description = "Returns all game objects of a specified type in the same area as the reference creature. " +
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
                Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject,
                Direction = GlyphPinDirection.Input
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
