using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;

/// <summary>Independent entry points sharing the trait and character context.</summary>
public abstract class TraitEffectResolutionStageExecutor : IGlyphNodeExecutor, IContextNodeProvider
{
    public abstract string TypeId { get; }
    public abstract string SourceDisplayName { get; }
    protected abstract string Description { get; }
    public virtual GlyphContextSchema Schema => OnTraitGrantedEventExecutor.Context;
    public string SourceTypeId => TypeId;
    public GlyphEventType? SourceEventType => GlyphEventType.TraitEffectResolution;
    public GlyphScriptCategory? SourceScriptCategory => GlyphScriptCategory.Trait;
    public List<ContextPinDescriptor> GetContextPins() => Schema.Fields.ToList();

    public Task<GlyphNodeResult> ExecuteAsync(GlyphNodeInstance node, GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput) => Task.FromResult(new GlyphNodeResult
        {
            NextExecPinId = "exec_out", OutputValues = Schema.Read(context)
        });

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = TypeId, DisplayName = SourceDisplayName, Description = Description,
        Category = "Trait Lifecycle", ColorClass = "node-stage", Archetype = GlyphNodeArchetype.PipelineStage,
        IsSingleton = true, RestrictToEventType = SourceEventType, ScriptCategory = SourceScriptCategory,
        OutputPins = [Pins.ExecOut("exec_out", "Then"), .. Schema.CreateOutputPins()], ContextSchema = Schema
    };
}

[GlyphNode]
public partial class TraitClientEnterStageExecutor : TraitEffectResolutionStageExecutor
{
    public const string NodeTypeId = "stage.trait_client_enter";
    public static GlyphEventDescriptor Event { get; } = new("trait.on_effect_resolution", GlyphEventType.TraitEffectResolution,
        GlyphScriptCategory.Trait, Stages: [
            new("main", TraitMainStageExecutor.NodeTypeId),
            new("client_enter", NodeTypeId),
            new("level_up", TraitLevelUpStageExecutor.NodeTypeId),
            new("respawn", TraitRespawnStageExecutor.NodeTypeId),
            new("confirmed", TraitConfirmedStageExecutor.NodeTypeId),
            new("death", TraitDeathStageExecutor.NodeTypeId)
        ], Capabilities: [typeof(TraitGlyphContext), typeof(GlyphCharacterContext)]);
    public override string TypeId => NodeTypeId;
    public override string SourceDisplayName => "Client Enter";
    protected override string Description => "Resolves trait effects when the player enters the module.";
}

[GlyphNode]
public partial class TraitMainStageExecutor : TraitEffectResolutionStageExecutor
{
    public const string NodeTypeId = "stage.trait_main";
    public override string TypeId => NodeTypeId;
    public override string SourceDisplayName => "Main";
    protected override string Description => "Runs before the matching lifecycle block on every trait effect rebuild. Does not run during death.";
}

[GlyphNode]
public partial class TraitLevelUpStageExecutor : TraitEffectResolutionStageExecutor
{
    public const string NodeTypeId = "stage.trait_level_up";
    public override string TypeId => NodeTypeId;
    public override string SourceDisplayName => "Level Up";
    protected override string Description => "Resolves trait effects when the player levels up.";
}

[GlyphNode]
public partial class TraitRespawnStageExecutor : TraitEffectResolutionStageExecutor
{
    public const string NodeTypeId = "stage.trait_respawn";
    public override string TypeId => NodeTypeId;
    public override string SourceDisplayName => "Respawn";
    protected override string Description => "Resolves trait effects when the player respawns.";
}

[GlyphNode]
public partial class TraitConfirmedStageExecutor : TraitEffectResolutionStageExecutor
{
    public const string NodeTypeId = "stage.trait_confirmed";
    public override string TypeId => NodeTypeId;
    public override string SourceDisplayName => "Confirmed";
    protected override string Description => "Resolves trait effects immediately after trait confirmation.";
}

[GlyphNode]
public partial class TraitDeathStageExecutor : TraitEffectResolutionStageExecutor
{
    public const string NodeTypeId = "stage.trait_death";
    private static readonly GlyphContextSchema DeathContext = new([
        .. OnTraitGrantedEventExecutor.Context.Fields,
        new("killer", "Killer", GlyphDataType.NwObject,
            ctx => ctx.Get<TraitGlyphContext>()?.Killer ?? NWN.Core.NWScript.OBJECT_INVALID)
    ]);
    public override string TypeId => NodeTypeId;
    public override string SourceDisplayName => "Death";
    public override GlyphContextSchema Schema => DeathContext;
    protected override string Description => "Runs trait death behavior without rebuilding permanent effects. Provides the killer.";
}
