using AmiaReforged.PwEngine.Features.Glyph.Core;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// The Glyph graph interpreter. Walks the node graph following execution (Exec) pin edges,
/// resolving data pin values lazily by tracing edges backward to source outputs.
/// Inspired by UE5's Blueprint VM execution model.
/// </summary>
public class GlyphInterpreter
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IGlyphNodeDefinitionRegistry _registry;
    private readonly Dictionary<string, IGlyphNodeExecutor> _executors;

    public GlyphInterpreter(
        IGlyphNodeDefinitionRegistry registry,
        IEnumerable<IGlyphNodeExecutor> executors)
    {
        _registry = registry;
        _executors = executors.ToDictionary(e => e.TypeId, StringComparer.Ordinal);
    }

    /// <summary>
    /// Executes a Glyph graph from its entry-point node to completion.
    /// </summary>
    /// <param name="context">The execution context containing the graph, encounter data, and mutable state</param>
    /// <returns>True if execution completed normally; false if cancelled, errored, or hit step limit</returns>
    public async Task<bool> ExecuteAsync(GlyphExecutionContext context)
    {
        GlyphGraph graph = context.Graph;

        // Find the entry-point event node
        GlyphNodeInstance? entryNode = graph.FindEntryNode();
        if (entryNode == null)
        {
            Log.Warn("[Glyph] Graph '{Name}' has no entry-point node for event type {EventType}. " +
                     "Nodes in graph: [{NodeTypes}]",
                graph.Name, graph.EventType,
                string.Join(", ", graph.Nodes.Select(n => n.TypeId)));
            return false;
        }

        Log.Debug("[Glyph] Found entry node '{TypeId}' ({Id}) for graph '{Name}'",
            entryNode.TypeId, entryNode.InstanceId, graph.Name);

        // Initialize graph variables from defaults
        InitializeVariables(context);

        Trace(context, $"Starting execution of graph '{graph.Name}' (event: {graph.EventType})");

        // Execute the entry-point node
        GlyphNodeResult entryResult = await ExecuteNode(entryNode, context);

        // Follow the execution chain from the entry node's output Exec pin
        if (entryResult.NextExecPinId != null)
        {
            await FollowExecChain(entryNode, entryResult.NextExecPinId, context);
        }

        Trace(context, $"Execution completed. Steps: {context.ExecutionStepCount}");
        return true;
    }

    /// <summary>
    /// Executes a specific pipeline stage within a multi-stage graph. Used for interaction
    /// pipeline graphs where a single graph contains multiple stage nodes (Attempted, Started,
    /// Tick, Completed), each invoked independently when its lifecycle event fires.
    /// </summary>
    /// <param name="context">The execution context containing the graph and mutable state</param>
    /// <param name="stageTypeId">The TypeId of the stage node to execute (e.g., "stage.interaction_attempted")</param>
    /// <returns>True if the stage executed normally; false if the stage node was not found or execution failed</returns>
    public async Task<bool> ExecuteStageAsync(GlyphExecutionContext context, string stageTypeId)
    {
        GlyphGraph graph = context.Graph;

        GlyphNodeInstance? stageNode = graph.FindStageNode(stageTypeId);
        if (stageNode == null)
        {
            Log.Warn("[Glyph] Graph '{Name}' has no stage node for '{StageTypeId}'. " +
                     "Nodes in graph: [{NodeTypes}]",
                graph.Name, stageTypeId,
                string.Join(", ", graph.Nodes.Select(n => n.TypeId)));
            return false;
        }

        Log.Debug("[Glyph] Found stage node '{TypeId}' ({Id}) for graph '{Name}'",
            stageNode.TypeId, stageNode.InstanceId, graph.Name);

        // Set the current pipeline stage so stage-aware nodes (e.g., FailInteraction) know
        // which phase is active
        context.CurrentPipelineStage = stageTypeId;

        // Initialize variables (idempotent for repeated calls within the same context)
        InitializeVariables(context);

        Trace(context, $"Starting stage '{stageTypeId}' of graph '{graph.Name}'");

        // Execute the stage node (same as an entry node — no exec_in, produces outputs + exec_out)
        GlyphNodeResult stageResult = await ExecuteNode(stageNode, context);

        // Follow the execution chain from the stage node's output Exec pin
        if (stageResult.NextExecPinId != null)
        {
            await FollowExecChain(stageNode, stageResult.NextExecPinId, context);
        }

        Trace(context, $"Stage '{stageTypeId}' completed. Steps: {context.ExecutionStepCount}");
        return true;
    }

    /// <summary>
    /// Follows the execution chain starting from a specific output Exec pin on a node.
    /// Uses a stack of <see cref="GlyphExecFrame"/>s to support multi-branch flow control
    /// (Sequence) and loops (ForEach).
    /// </summary>
    private async Task FollowExecChain(
        GlyphNodeInstance sourceNode,
        string execPinId,
        GlyphExecutionContext context)
    {
        Stack<GlyphExecFrame> stack = new();
        Guid currentNodeId = sourceNode.InstanceId;
        string currentPinId = execPinId;

        while (true)
        {
            // Check cancellation and step limit
            if (context.CancellationToken.IsCancellationRequested)
            {
                Trace(context, "Execution cancelled via CancellationToken.");
                return;
            }

            if (context.ExecutionStepCount >= context.MaxExecutionSteps)
            {
                Log.Warn("Glyph graph '{Name}' hit execution step limit ({Limit}). Possible infinite loop.",
                    context.Graph.Name, context.MaxExecutionSteps);
                Trace(context, $"Execution halted: step limit {context.MaxExecutionSteps} reached.");
                return;
            }

            // Find the edge from the current Exec output pin
            GlyphEdge? edge = context.Graph.GetEdgesFrom(currentNodeId, currentPinId).FirstOrDefault();
            if (edge == null)
            {
                // No outgoing edge — this branch terminates.
                // Check if there's a frame on the stack to resume.
                (Guid nodeId, string pinId)? resume = await TryResumeFromStack(stack, context);
                if (resume == null) return; // Stack empty — execution complete
                (currentNodeId, currentPinId) = resume.Value;
                continue;
            }

            // Get the target node
            GlyphNodeInstance? targetNode = context.Graph.GetNode(edge.TargetNodeId);
            if (targetNode == null)
            {
                Log.Warn("Glyph edge targets non-existent node {NodeId}.", edge.TargetNodeId);
                (Guid nodeId, string pinId)? resume = await TryResumeFromStack(stack, context);
                if (resume == null) return;
                (currentNodeId, currentPinId) = resume.Value;
                continue;
            }

            // Execute the target node
            GlyphNodeResult result = await ExecuteNode(targetNode, context);

            if (result.NextExecPinId == null)
            {
                // Node terminates this branch — check stack
                (Guid nodeId, string pinId)? resume = await TryResumeFromStack(stack, context);
                if (resume == null) return;
                (currentNodeId, currentPinId) = resume.Value;
                continue;
            }

            // Handle multi-branch results (Sequence-style)
            if (result.BranchPinIds is { Length: > 0 })
            {
                // Push a frame with the remaining branches
                GlyphExecFrame frame = new()
                {
                    Node = targetNode,
                    RemainingBranches = new Queue<string>(result.BranchPinIds),
                };
                stack.Push(frame);
                Trace(context, $"Pushed Sequence frame for node {targetNode.TypeId} with {result.BranchPinIds.Length} remaining branches.");
            }

            // Handle loop results (ForEach-style)
            if (result.IsLoopNode)
            {
                // Push a loop frame — the interpreter will re-execute this node when the body terminates
                GlyphExecFrame loopFrame = new()
                {
                    Node = targetNode,
                    IsLoop = true,
                };
                stack.Push(loopFrame);
                Trace(context, $"Pushed Loop frame for node {targetNode.TypeId}.");
            }

            // Continue to the next node in the chain
            currentNodeId = targetNode.InstanceId;
            currentPinId = result.NextExecPinId;
        }
    }

    /// <summary>
    /// Attempts to resume execution from the stack after a branch terminates.
    /// For Sequence frames: follows the next remaining branch pin.
    /// For Loop frames: re-executes the loop node to check for the next iteration.
    /// Returns null if the stack is empty (execution complete), or the (nodeId, pinId) to continue from.
    /// </summary>
    private async Task<(Guid nodeId, string pinId)?> TryResumeFromStack(
        Stack<GlyphExecFrame> stack,
        GlyphExecutionContext context)
    {
        while (stack.Count > 0)
        {
            GlyphExecFrame frame = stack.Peek();

            if (frame.IsLoop)
            {
                // Re-execute the loop node to advance the iteration
                // Clear cached outputs so the node produces fresh values for this iteration
                ClearNodeOutputCache(frame.Node, context);

                GlyphNodeResult loopResult = await ExecuteNode(frame.Node, context);

                if (loopResult.IsLoopNode && loopResult.NextExecPinId != null)
                {
                    // Another iteration — follow the loop body again
                    Trace(context, $"Loop node {frame.Node.TypeId} continuing iteration.");
                    return (frame.Node.InstanceId, loopResult.NextExecPinId);
                }

                // Loop finished — pop the frame
                stack.Pop();
                Trace(context, $"Loop node {frame.Node.TypeId} completed all iterations.");

                if (loopResult.NextExecPinId != null)
                {
                    // Follow the completed pin (e.g., "completed" on ForEach)
                    return (frame.Node.InstanceId, loopResult.NextExecPinId);
                }

                // Loop returned Done() — try the next frame on the stack
                continue;
            }

            // Sequence-style frame: dequeue the next branch
            if (frame.RemainingBranches.Count > 0)
            {
                string nextBranch = frame.RemainingBranches.Dequeue();
                Trace(context, $"Sequence frame resuming: following branch '{nextBranch}' ({frame.RemainingBranches.Count} remaining).");
                return (frame.Node.InstanceId, nextBranch);
            }

            // Frame exhausted — pop and try the next one
            stack.Pop();
        }

        return null;
    }

    /// <summary>
    /// Clears cached output values for a node so it can be re-evaluated (used for loop iterations).
    /// </summary>
    private static void ClearNodeOutputCache(GlyphNodeInstance node, GlyphExecutionContext context)
    {
        string prefix = $"{node.InstanceId}:";
        List<string> keysToRemove = context.PinValueCache.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (string key in keysToRemove)
        {
            context.PinValueCache.Remove(key);
        }
    }

    /// <summary>
    /// Executes a single node: finds its executor, resolves input data pins, and runs it.
    /// </summary>
    private async Task<GlyphNodeResult> ExecuteNode(
        GlyphNodeInstance node,
        GlyphExecutionContext context)
    {
        context.ExecutionStepCount++;
        Trace(context, $"Step {context.ExecutionStepCount}: Executing node '{node.TypeId}' ({node.InstanceId})");

        if (!_executors.TryGetValue(node.TypeId, out IGlyphNodeExecutor? executor))
        {
            Log.Warn("[Glyph] No executor registered for node type '{TypeId}'. Registered types: [{Types}]",
                node.TypeId, string.Join(", ", _executors.Keys));
            return GlyphNodeResult.Done();
        }

        // Create the input resolver — lazily evaluates data pins by tracing edges
        async Task<object?> ResolveInput(string inputPinId)
        {
            return await ResolveInputPinValue(node, inputPinId, context);
        }

        try
        {
            GlyphNodeResult result = await executor.ExecuteAsync(node, context, ResolveInput);

            // Cache any output values for downstream consumers
            foreach (KeyValuePair<string, object?> kvp in result.OutputValues)
            {
                context.CachePinValue(node.InstanceId, kvp.Key, kvp.Value);
                Trace(context, $"  Output [{kvp.Key}] = {FormatValue(kvp.Value)}");
            }

            if (result.NextExecPinId != null)
            {
                Trace(context, $"  -> Following exec pin '{result.NextExecPinId}'");
            }

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[Glyph] Error executing node '{TypeId}' ({InstanceId}) in graph '{Graph}'.",
                node.TypeId, node.InstanceId, context.Graph.Name);
            Trace(context, $"ERROR in node {node.TypeId}: {ex.Message}");
            return GlyphNodeResult.Done();
        }
    }

    /// <summary>
    /// Resolves the value of an input data pin by tracing the edge back to the source
    /// node's output pin. If the pin has no incoming edge, returns the pin's default value.
    /// Uses caching to avoid re-evaluating the same source node multiple times.
    /// </summary>
    private async Task<object?> ResolveInputPinValue(
        GlyphNodeInstance node,
        string inputPinId,
        GlyphExecutionContext context)
    {
        // Check if there's an incoming edge to this pin
        GlyphEdge? edge = context.Graph.GetEdgeTo(node.InstanceId, inputPinId);
        if (edge == null)
        {
            // No edge — use property override if available, otherwise pin default
            if (node.PropertyOverrides.TryGetValue(inputPinId, out string? overrideValue))
            {
                object? parsed = ParseDefaultValue(node.TypeId, inputPinId, overrideValue);
                Trace(context, $"  Input [{inputPinId}] resolved from property override: {FormatValue(parsed)}");
                return parsed;
            }

            object? defaultVal = GetPinDefaultValue(node.TypeId, inputPinId);
            Trace(context, $"  Input [{inputPinId}] resolved from default: {FormatValue(defaultVal)}");
            return defaultVal;
        }

        // Check the cache first
        if (context.TryGetCachedPinValue(edge.SourceNodeId, edge.SourcePinId, out object? cachedValue))
        {
            Trace(context, $"  Input [{inputPinId}] resolved from cache (source {edge.SourceNodeId}:{edge.SourcePinId}): {FormatValue(cachedValue)}");
            return cachedValue;
        }

        // Need to evaluate the source node (for pure data nodes that haven't been executed yet)
        GlyphNodeInstance? sourceNode = context.Graph.GetNode(edge.SourceNodeId);
        if (sourceNode == null)
        {
            Log.Warn("Edge source node {NodeId} not found in graph.", edge.SourceNodeId);
            return null;
        }

        // Execute the source node to get its output values
        GlyphNodeResult sourceResult = await ExecuteNode(sourceNode, context);

        // Return the specific output pin value
        return sourceResult.OutputValues.GetValueOrDefault(edge.SourcePinId);
    }

    /// <summary>
    /// Gets the default value for a pin from its definition.
    /// </summary>
    private object? GetPinDefaultValue(string nodeTypeId, string pinId)
    {
        GlyphNodeDefinition? definition = _registry.Get(nodeTypeId);
        GlyphPin? pin = definition?.InputPins.FirstOrDefault(p => p.Id == pinId);
        if (pin?.DefaultValue == null) return null;
        return ParseDefaultValue(nodeTypeId, pinId, pin.DefaultValue);
    }

    /// <summary>
    /// Parses a string default value to the appropriate .NET type based on the pin's data type.
    /// </summary>
    private object? ParseDefaultValue(string nodeTypeId, string pinId, string value)
    {
        GlyphNodeDefinition? definition = _registry.Get(nodeTypeId);
        GlyphPin? pin = definition?.InputPins.FirstOrDefault(p => p.Id == pinId)
                        ?? definition?.OutputPins.FirstOrDefault(p => p.Id == pinId);

        if (pin == null) return value;

        return pin.DataType switch
        {
            GlyphDataType.Bool => bool.TryParse(value, out bool b) ? b : false,
            GlyphDataType.Int => int.TryParse(value, out int i) ? i : 0,
            GlyphDataType.Float => double.TryParse(value, out double d) ? d : 0.0,
            GlyphDataType.String => value,
            _ => value
        };
    }

    /// <summary>
    /// Initializes the execution context's variable store from graph variable defaults.
    /// </summary>
    private static void InitializeVariables(GlyphExecutionContext context)
    {
        foreach (GlyphVariable variable in context.Graph.Variables)
        {
            object? defaultValue = variable.DataType switch
            {
                GlyphDataType.Bool => variable.DefaultValue != null && bool.TryParse(variable.DefaultValue, out bool b) ? b : false,
                GlyphDataType.Int => variable.DefaultValue != null && int.TryParse(variable.DefaultValue, out int i) ? i : 0,
                GlyphDataType.Float => variable.DefaultValue != null && double.TryParse(variable.DefaultValue, out double d) ? d : 0.0,
                GlyphDataType.String => variable.DefaultValue ?? string.Empty,
                _ => null
            };

            context.Variables[variable.Name] = defaultValue;
        }
    }

    private static void Trace(GlyphExecutionContext context, string message)
    {
        if (context.EnableTracing)
        {
            context.TraceLog.Add($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
        }
    }

    private static string FormatValue(object? value)
    {
        if (value == null) return "(null)";
        if (value is uint u) return $"0x{u:X} ({u})";
        if (value is string s) return $"\"{s}\"";
        return value.ToString() ?? "(null)";
    }
}
