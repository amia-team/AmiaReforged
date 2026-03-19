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
/// <b>Input:</b> a trigger object (NwObject — wire from <c>target_id</c> or any other game object pin
/// that references a <c>worldengine_node_region</c>-tagged trigger).<br/>
/// <b>Outputs:</b> whether the spawn succeeded, plus full details about the created node.
/// </para>
/// <para>
/// If the provided object is not a valid <c>worldengine_node_region</c> trigger, or the area has
/// no matching definitions, the node sets <c>success</c> to <c>false</c> — it never throws.
/// The scripter decides what to do with the failure branch.
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
        uint triggerHandle = Convert.ToUInt32(triggerValue ?? 0);

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
            SpawnResourceNodeResult? result = context.WorldEngine.SpawnResourceNode(triggerHandle);

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
        Description = "Spawns a single resource node inside a worldengine_node_region trigger, pulling " +
                      "from the area's resource definitions filtered by the trigger's node_tags. If the " +
                      "object is not a valid trigger or no matching definitions exist, success is false.",
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
                Id = "trigger", Name = "Trigger", DataType = GlyphDataType.NwObject,
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
