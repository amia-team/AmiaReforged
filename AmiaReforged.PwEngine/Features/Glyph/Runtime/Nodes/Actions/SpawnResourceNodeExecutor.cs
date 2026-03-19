using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;

/// <summary>
/// Spawns a single resource node inside a trigger zone, using the area's resource definitions
/// filtered by the trigger's <c>node_tags</c> local variable.
/// <para>
/// This node is the Glyph-accessible equivalent of the provisioning pipeline used by the economy
/// bootstrapper. It allows interaction graphs (e.g. prospecting, foraging) to create context-relevant
/// resource nodes without hard-coding any assumptions about the interaction type.
/// </para>
/// <para>
/// <b>Inputs:</b> a trigger UUID (String — wire from <c>target_id</c> on a stage node) and
/// the area ResRef (String — wire from <c>area_resref</c>).<br/>
/// <b>Outputs:</b> whether the spawn succeeded, plus full details about the created node.
/// </para>
/// <para>
/// On failure (missing trigger, no matching definitions, etc.), the node sets <c>success</c> to
/// <c>false</c> and logs the reason to the server — it never throws.
/// </para>
/// </summary>
public class SpawnResourceNodeExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "action.spawn_resource_node";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? triggerValue = await resolveInput("trigger");
        object? areaValue = await resolveInput("area_resref");

        string triggerUuid = triggerValue?.ToString() ?? string.Empty;
        string areaResRef = areaValue?.ToString() ?? string.Empty;

        Dictionary<string, object?> outputs = new()
        {
            ["success"] = false,
            ["node_id"] = string.Empty,
            ["node_name"] = string.Empty,
            ["definition_tag"] = string.Empty,
            ["quality"] = string.Empty,
            ["uses"] = 0,
            ["spawn_x"] = 0f,
            ["spawn_y"] = 0f,
            ["spawn_z"] = 0f,
        };

        if (context.WorldEngine != null)
        {
            SpawnResourceNodeResult? result = context.WorldEngine.SpawnResourceNode(triggerUuid, areaResRef);

            if (result != null)
            {
                outputs["success"] = true;
                outputs["node_id"] = result.NodeId.ToString();
                outputs["node_name"] = result.Name;
                outputs["definition_tag"] = result.DefinitionTag;
                outputs["quality"] = result.QualityLabel;
                outputs["uses"] = result.Uses;
                outputs["spawn_x"] = result.X;
                outputs["spawn_y"] = result.Y;
                outputs["spawn_z"] = result.Z;
            }
        }

        return new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs,
        };
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Spawn Resource Node",
        Category = "Actions",
        Description = "Spawns a single resource node inside a trigger zone, pulling from the area's " +
                      "resource definitions filtered by the trigger's node_tags. Logs failures to the " +
                      "server without crashing. Suitable for prospecting, foraging, or any interaction " +
                      "that needs to create context-relevant resource nodes.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin
            {
                Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec,
                Direction = GlyphPinDirection.Input,
            },
            new GlyphPin
            {
                Id = "trigger", Name = "Trigger (UUID)", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input,
            },
            new GlyphPin
            {
                Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input,
            },
        ],
        OutputPins =
        [
            new GlyphPin
            {
                Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "success", Name = "Success", DataType = GlyphDataType.Bool,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "node_id", Name = "Node ID", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "node_name", Name = "Node Name", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "definition_tag", Name = "Definition Tag", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "quality", Name = "Quality", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "uses", Name = "Uses", DataType = GlyphDataType.Int,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "spawn_x", Name = "Spawn X", DataType = GlyphDataType.Float,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "spawn_y", Name = "Spawn Y", DataType = GlyphDataType.Float,
                Direction = GlyphPinDirection.Output,
            },
            new GlyphPin
            {
                Id = "spawn_z", Name = "Spawn Z", DataType = GlyphDataType.Float,
                Direction = GlyphPinDirection.Output,
            },
        ],
    };
}
