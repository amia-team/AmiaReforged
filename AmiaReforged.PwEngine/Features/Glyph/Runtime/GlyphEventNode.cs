using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

public abstract class GlyphEventNode : IGlyphNodeExecutor, IContextNodeProvider
{
    protected abstract GlyphEventDescriptor EventContract { get; }
    protected abstract string Description { get; }
    public abstract GlyphContextSchema Schema { get; }
    public abstract string SourceDisplayName { get; }
    public string TypeId => EventContract.Entry();
    public string SourceTypeId => TypeId;
    public GlyphEventType? SourceEventType => EventContract.EventType;
    public GlyphScriptCategory? SourceScriptCategory => EventContract.Category;
    public List<ContextPinDescriptor> GetContextPins() => Schema.Fields.ToList();
    public Task<GlyphNodeResult> ExecuteAsync(GlyphNodeInstance node, GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput) => Task.FromResult(new GlyphNodeResult
        {
            NextExecPinId = "exec_out", OutputValues = Schema.Read(context)
        });
    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = SourceDisplayName, Description = Description,
        Category = "Events", ColorClass = "node-event", Archetype = GlyphNodeArchetype.EventEntry,
        IsSingleton = true, RestrictToEventType = SourceEventType, ScriptCategory = SourceScriptCategory,
        OutputPins = [Pins.ExecOut("exec_out", "Execute"), .. Schema.CreateOutputPins()], ContextSchema = Schema
    };
}
