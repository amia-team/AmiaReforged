using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// Branch node — the Glyph equivalent of an if/else statement.
/// Evaluates a boolean condition input and follows either the True or False Exec output.
/// </summary>
public sealed class BranchExecutor : GlyphNodeBase
{
    public const string NodeTypeId = "flow.branch";

    public override string TypeId => NodeTypeId;

    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        bool condition = await cx.InBool("condition");
        return GlyphNodeResult.Continue(condition ? "true" : "false");
    }

    /// <summary>
    /// Creates the node definition for registration in the registry.
    /// </summary>
    public override GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Branch",
        Category = "Flow Control",
        Description = "If/else branch. Evaluates the condition and follows the True or False path.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        InputPins =
        [
            Pins.ExecIn(),
            Pins.InBool("condition", "Condition"),
        ],
        OutputPins =
        [
            Pins.ExecOut("true", "True"),
            Pins.ExecOut("false", "False"),
        ]
    };
}
