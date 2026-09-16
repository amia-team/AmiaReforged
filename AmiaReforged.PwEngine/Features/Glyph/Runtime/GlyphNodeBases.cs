using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Ergonomic base for node executors. Implements <see cref="IGlyphNodeExecutor.ExecuteAsync"/>
/// by wrapping the raw resolver in a <see cref="GlyphNodeContext"/>, so subclasses only
/// implement <see cref="RunAsync"/> with typed input access.
/// </summary>
public abstract class GlyphNodeBase : IGlyphNodeExecutor
{
    public abstract string TypeId { get; }

    public abstract GlyphNodeDefinition CreateDefinition();

    /// <summary>
    /// Node logic using the typed <paramref name="cx"/> facade.
    /// </summary>
    public abstract Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx);

    Task<GlyphNodeResult> IGlyphNodeExecutor.ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput) =>
        RunAsync(new GlyphNodeContext(node, context, resolveInput));
}

/// <summary>
/// Base for pure data nodes (no exec pins). The subclass returns output values only;
/// the framework wraps them in <see cref="GlyphNodeResult.Data"/>.
/// </summary>
public abstract class GlyphPureNode : GlyphNodeBase
{
    public sealed override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx) =>
        GlyphNodeResult.Data(await RunPureAsync(cx));

    /// <summary>
    /// Computes output pin values keyed by output pin ID.
    /// </summary>
    protected abstract Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx);
}

/// <summary>
/// Base for linear action nodes with a single exec-out pin. The subclass performs
/// its side effects; the framework continues along <see cref="OutPinId"/>.
/// Nodes with multiple exec outputs (branches, loops) should extend
/// <see cref="GlyphNodeBase"/> directly.
/// </summary>
public abstract class GlyphActionNode : GlyphNodeBase
{
    /// <summary>
    /// The exec output pin to follow after the action. Defaults to "exec_out".
    /// </summary>
    protected virtual string OutPinId => "exec_out";

    public sealed override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)
    {
        await RunActionAsync(cx);
        return GlyphNodeResult.Continue(OutPinId);
    }

    protected abstract Task RunActionAsync(GlyphNodeContext cx);
}
