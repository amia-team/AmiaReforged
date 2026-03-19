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

        // Read configurable messages from property overrides (or use defaults)
        string successMessage = node.PropertyOverrides.TryGetValue("success_message", out string? sm) && !string.IsNullOrWhiteSpace(sm)
            ? sm
            : "You discovered a new resource!";
        string failureMessage = node.PropertyOverrides.TryGetValue("failure_message", out string? fm) && !string.IsNullOrWhiteSpace(fm)
            ? fm
            : "There are no more resources of this type to be found here.";

        Dictionary<string, object?> outputs = new()
        {
            ["success"] = false,
            ["message"] = string.Empty,
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
            SpawnResourceNodeOutcome outcome = context.WorldEngine.SpawnResourceNode(triggerHandle);

            if (outcome.Success && outcome.Result != null)
            {
                outputs["success"] = true;
                outputs["message"] = successMessage;
                outputs["node_id"] = outcome.Result.NodeId.ToString();
                outputs["node_name"] = outcome.Result.Name;
                outputs["definition_tag"] = outcome.Result.DefinitionTag;
                outputs["quality"] = outcome.Result.QualityLabel;
                outputs["uses"] = outcome.Result.Uses;
                outputs["spawn_x"] = outcome.Result.X;
                outputs["spawn_y"] = outcome.Result.Y;
                outputs["spawn_z"] = outcome.Result.Z;
            }
            else
            {
                outputs["message"] = failureMessage;
            }
        }
        else
        {
            outputs["message"] = failureMessage;
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
        Properties =
        [
            new GlyphPropertyDefinition
            {
                Id = "success_message",
                DisplayName = "Success Message",
                DefaultValue = "You discovered a new resource!",
            },
            new GlyphPropertyDefinition
            {
                Id = "failure_message",
                DisplayName = "Failure Message",
                DefaultValue = "There are no more resources of this type to be found here.",
            },
        ],
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
                Id = "message", Name = "Message", DataType = GlyphDataType.String,
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
