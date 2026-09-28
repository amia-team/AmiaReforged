using AmiaReforged.PwEngine.Features.Glyph.Core;
using Anvil.API;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Returns the nearest game object of a single curated type from an origin object.
/// Pure data node — no execution flow. Returns <see cref="NWScript.OBJECT_INVALID"/> when
/// the origin is invalid or unresolvable, the type is not one of the five supported types,
/// or no matching object exists.
/// <para>
/// Supported types (case-insensitive; lowercase canonical): <c>trigger</c>, <c>door</c>,
/// <c>placeable</c>, <c>creature</c>, <c>waypoint</c>. This is the singular counterpart of
/// <see cref="GetNearestObjectsByTypeExecutor"/>, which returns a list.
/// </para>
/// </summary>
public class GetNearestObjectByTypeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.nearest_object_by_type";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? typeValue = await resolveInput("type");
        string type = typeValue?.ToString() ?? string.Empty;

        // Validate the curated type before touching Anvil so an unknown type is cheap,
        // safe, and testable without a running server.
        if (!NwObjectQuery.IsCuratedType(type))
        {
            return InvalidResult();
        }

        uint originId = ConvertId(await resolveInput("origin"));

        // Guard: NWN uses 0x7F000000 as OBJECT_INVALID; treat 0 the same way.
        if (originId == 0 || originId == NWScript.OBJECT_INVALID)
        {
            return InvalidResult();
        }

        NwGameObject? origin = originId.ToNwObject<NwGameObject>();
        if (origin == null)
        {
            return InvalidResult();
        }

        uint objectId = NwObjectQuery.NearestObjectId(origin, type);

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["object"] = objectId
        });
    }

    private static uint ConvertId(object? value) => value is null ? 0u : Convert.ToUInt32(value);

    private static GlyphNodeResult InvalidResult() => GlyphNodeResult.Data(new Dictionary<string, object?>
    {
        ["object"] = NWScript.OBJECT_INVALID
    });

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Nearest Object By Type",
        Category = "Getters",
        Description = "Returns the nearest game object of a curated type from an origin. " +
                      "Supported types (case-insensitive, lowercase canonical): trigger, door, " +
                      "placeable, creature, waypoint. Returns OBJECT_INVALID when the origin is " +
                      "invalid, the type is unsupported, or no match exists.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins =
        [
            new GlyphPin
            {
                Id = "origin", Name = "Origin", DataType = GlyphDataType.NwObject,
                Direction = GlyphPinDirection.Input
            },
            new GlyphPin
            {
                Id = "type", Name = "Object Type", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input
            }
        ],
        OutputPins =
        [
            new GlyphPin
            {
                Id = "object", Name = "Object", DataType = GlyphDataType.NwObject,
                Direction = GlyphPinDirection.Output
            }
        ]
    };
}
