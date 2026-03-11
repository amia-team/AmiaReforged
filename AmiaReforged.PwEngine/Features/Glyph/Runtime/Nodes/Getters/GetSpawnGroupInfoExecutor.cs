using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Gets information about the current spawn group from the execution context.
/// Reads directly from the context — no creature input needed. Pure data node.
/// Only meaningful during encounter events (BeforeGroupSpawn, AfterGroupSpawn, OnCreatureSpawn).
/// </summary>
public class GetSpawnGroupInfoExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.spawn_group_info";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        string groupName = context.Group?.Name ?? string.Empty;
        int weight = context.Group?.Weight ?? 0;
        string distributionMethod = context.Group?.DistributionMethod.ToString() ?? string.Empty;
        int entryCount = context.Group?.Entries.Count ?? 0;
        bool overrideMutations = context.Group?.OverrideMutations ?? false;

        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["group_name"] = groupName,
            ["weight"] = weight,
            ["distribution_method"] = distributionMethod,
            ["entry_count"] = entryCount,
            ["override_mutations"] = overrideMutations
        }));
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Spawn Group Info",
        Category = "Getters",
        Description = "Returns information about the current spawn group: name, weight, distribution method, " +
                      "entry count, and whether mutations are overridden. Only meaningful during encounter events.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "group_name", Name = "Group Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "weight", Name = "Weight", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "distribution_method", Name = "Distribution Method", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "entry_count", Name = "Entry Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "override_mutations", Name = "Override Mutations", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output }
        ]
    };
}
