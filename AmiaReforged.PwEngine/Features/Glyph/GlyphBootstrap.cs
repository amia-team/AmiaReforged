using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Constants;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph;

/// <summary>
/// Bootstraps the Glyph visual scripting system at module load.
/// Registers all built-in node definitions and executors, then creates the
/// <see cref="GlyphInterpreter"/> singleton used by the encounter hook service.
/// </summary>
[ServiceBinding(typeof(GlyphBootstrap))]
public class GlyphBootstrap
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// The shared <see cref="GlyphInterpreter"/> instance. Initialized at startup,
    /// used by the encounter hook service to execute graphs.
    /// </summary>
    public GlyphInterpreter Interpreter { get; }

    public GlyphBootstrap(IGlyphNodeDefinitionRegistry registry)
    {
        Log.Info("Bootstrapping Glyph visual scripting system...");

        // Register all built-in node definitions
        RegisterDefinitions(registry);

        // Create all built-in node executors
        List<IGlyphNodeExecutor> executors = CreateExecutors();

        // Create the interpreter
        Interpreter = new GlyphInterpreter(registry, executors);

        Log.Info("Glyph bootstrap complete. {DefCount} definitions registered, {ExecCount} executors loaded.",
            registry.GetAll().Count, executors.Count);
    }

    private static void RegisterDefinitions(IGlyphNodeDefinitionRegistry registry)
    {
        // Event nodes
        registry.Register(BeforeGroupSpawnEventExecutor.CreateDefinition());
        registry.Register(AfterGroupSpawnEventExecutor.CreateDefinition());
        registry.Register(OnCreatureDeathEventExecutor.CreateDefinition());
        registry.Register(OnCreatureSpawnEventExecutor.CreateDefinition());
        registry.Register(OnBossSpawnEventExecutor.CreateDefinition());
        registry.Register(OnTraitGrantedEventExecutor.CreateDefinition());
        registry.Register(OnTraitRemovedEventExecutor.CreateDefinition());

        // Flow control
        registry.Register(BranchExecutor.CreateDefinition());
        registry.Register(ForEachExecutor.CreateDefinition());
        registry.Register(SequenceExecutor.CreateDefinition());
        registry.Register(DoNothingExecutor.CreateDefinition());

        // Actions
        registry.Register(ApplyEffectExecutor.CreateDefinition());
        registry.Register(ModifySpawnCountExecutor.CreateDefinition());
        registry.Register(CancelSpawnExecutor.CreateDefinition());
        registry.Register(SendFloatingTextExecutor.CreateDefinition());
        registry.Register(SetLocalVariableExecutor.CreateDefinition());
        registry.Register(DespawnCreatureExecutor.CreateDefinition());
        registry.Register(SkipBonusesExecutor.CreateDefinition());
        registry.Register(SkipMutationsExecutor.CreateDefinition());
        registry.Register(SetCreatureNameExecutor.CreateDefinition());
        registry.Register(HealCreatureExecutor.CreateDefinition());
        registry.Register(DamageCreatureExecutor.CreateDefinition());

        // Constants
        registry.Register(StringConstantExecutor.CreateDefinition());
        registry.Register(IntConstantExecutor.CreateDefinition());
        registry.Register(FloatConstantExecutor.CreateDefinition());
        registry.Register(BoolConstantExecutor.CreateDefinition());

        // Getters
        registry.Register(GetCreatureHPExecutor.CreateDefinition());
        registry.Register(GetPartySizeExecutor.CreateDefinition());
        registry.Register(GetChaosStateExecutor.CreateDefinition());
        registry.Register(GetTimeOfDayExecutor.CreateDefinition());
        registry.Register(GetRandomIntExecutor.CreateDefinition());
        registry.Register(GetAreaResRefExecutor.CreateDefinition());
        registry.Register(GetRegionInfoExecutor.CreateDefinition());
        registry.Register(GetCreatureLevelExecutor.CreateDefinition());
        registry.Register(GetPartyMembersExecutor.CreateDefinition());
        registry.Register(GetLocalVariableExecutor.CreateDefinition());
        registry.Register(GetCreatureResRefExecutor.CreateDefinition());
        registry.Register(GetCreatureNameExecutor.CreateDefinition());
        registry.Register(GetCreatureACExecutor.CreateDefinition());
        registry.Register(GetCreatureAbilityScoreExecutor.CreateDefinition());
        registry.Register(GetCreatureRaceExecutor.CreateDefinition());
        registry.Register(GetSpawnGroupInfoExecutor.CreateDefinition());
        registry.Register(GetTriggeringPlayerExecutor.CreateDefinition());

        // Math / Logic
        registry.Register(CompareExecutor.CreateDefinition());
        registry.Register(MathOpExecutor.CreateDefinition());
        registry.Register(BooleanOpExecutor.CreateDefinition());
        registry.Register(NotExecutor.CreateDefinition());

        // Traits
        registry.Register(HasTraitExecutor.CreateDefinition());
        registry.Register(GetCreatureTraitsExecutor.CreateDefinition());

        // Interaction pipeline stages
        registry.Register(InteractionAttemptedStageExecutor.CreateDefinition());
        registry.Register(InteractionStartedStageExecutor.CreateDefinition());
        registry.Register(InteractionTickStageExecutor.CreateDefinition());
        registry.Register(InteractionCompletedStageExecutor.CreateDefinition());
        registry.Register(FailInteractionExecutor.CreateDefinition());
        registry.Register(GetInteractionInfoExecutor.CreateDefinition());
        registry.Register(SuppressEventExecutor.CreateDefinition());
        registry.Register(SetProgressExecutor.CreateDefinition());
        registry.Register(SetRequiredRoundsExecutor.CreateDefinition());
        registry.Register(SetStatusExecutor.CreateDefinition());
        registry.Register(SetMetadataExecutor.CreateDefinition());
        registry.Register(GetMetadataExecutor.CreateDefinition());
        registry.Register(SkillCheckExecutor.CreateDefinition());
        registry.Register(PlayVfxExecutor.CreateDefinition());
        registry.Register(SendMessageExecutor.CreateDefinition());
        registry.Register(HasItemExecutor.CreateDefinition());
    }

    private static List<IGlyphNodeExecutor> CreateExecutors() =>
    [
        // Events
        new BeforeGroupSpawnEventExecutor(),
        new AfterGroupSpawnEventExecutor(),
        new OnCreatureDeathEventExecutor(),
        new OnCreatureSpawnEventExecutor(),
        new OnBossSpawnEventExecutor(),
        new OnTraitGrantedEventExecutor(),
        new OnTraitRemovedEventExecutor(),

        // Flow
        new BranchExecutor(),
        new ForEachExecutor(),
        new SequenceExecutor(),
        new DoNothingExecutor(),

        // Actions
        new ApplyEffectExecutor(),
        new ModifySpawnCountExecutor(),
        new CancelSpawnExecutor(),
        new SendFloatingTextExecutor(),
        new SetLocalVariableExecutor(),
        new DespawnCreatureExecutor(),
        new SkipBonusesExecutor(),
        new SkipMutationsExecutor(),
        new SetCreatureNameExecutor(),
        new HealCreatureExecutor(),
        new DamageCreatureExecutor(),

        // Constants
        new StringConstantExecutor(),
        new IntConstantExecutor(),
        new FloatConstantExecutor(),
        new BoolConstantExecutor(),

        // Getters
        new GetCreatureHPExecutor(),
        new GetPartySizeExecutor(),
        new GetChaosStateExecutor(),
        new GetTimeOfDayExecutor(),
        new GetRandomIntExecutor(),
        new GetAreaResRefExecutor(),
        new GetRegionInfoExecutor(),
        new GetCreatureLevelExecutor(),
        new GetPartyMembersExecutor(),
        new GetLocalVariableExecutor(),
        new GetCreatureResRefExecutor(),
        new GetCreatureNameExecutor(),
        new GetCreatureACExecutor(),
        new GetCreatureAbilityScoreExecutor(),
        new GetCreatureRaceExecutor(),
        new GetSpawnGroupInfoExecutor(),
        new GetTriggeringPlayerExecutor(),

        // Math / Logic
        new CompareExecutor(),
        new MathOpExecutor(),
        new BooleanOpExecutor(),
        new NotExecutor(),

        // Traits
        new HasTraitExecutor(),
        new GetCreatureTraitsExecutor(),

        // Interaction pipeline stages
        new InteractionAttemptedStageExecutor(),
        new InteractionStartedStageExecutor(),
        new InteractionTickStageExecutor(),
        new InteractionCompletedStageExecutor(),
        new FailInteractionExecutor(),
        new GetInteractionInfoExecutor(),
        new SuppressEventExecutor(),
        new SetProgressExecutor(),
        new SetRequiredRoundsExecutor(),
        new SetStatusExecutor(),
        new SetMetadataExecutor(),
        new GetMetadataExecutor(),
        new SkillCheckExecutor(),
        new PlayVfxExecutor(),
        new SendMessageExecutor(),
        new HasItemExecutor()
    ];
}
