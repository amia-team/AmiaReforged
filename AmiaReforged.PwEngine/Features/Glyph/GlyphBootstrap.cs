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

        // Create all built-in node executors — single authoritative list
        List<IGlyphNodeExecutor> executors = CreateExecutors();

        // Auto-register definitions from each executor (eliminates the old dual-list)
        foreach (IGlyphNodeExecutor executor in executors)
        {
            registry.Register(executor.CreateDefinition());
        }

        // Create the interpreter
        Interpreter = new GlyphInterpreter(registry, executors);

        Log.Info("Glyph bootstrap complete. {DefCount} definitions registered, {ExecCount} executors loaded.",
            registry.GetAll().Count, executors.Count);
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
        new BreakExecutor(),

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
        new GetNearestObjectsByTypeExecutor(),
        new GetTagExecutor(),
        new GetObjectResRefExecutor(),
        new GetDistanceBetweenExecutor(),
        new SplitStringExecutor(),

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
        new HasItemExecutor(),

        // Industries & Knowledge
        new GetIndustryMembershipsExecutor(),
        new GetIndustryLevelExecutor(),
        new IsIndustryMemberExecutor(),
        new HasKnowledgeExecutor(),
        new HasUnlockedInteractionExecutor(),
        new GetKnowledgeProgressionExecutor(),
        new GetLearnedKnowledgeExecutor()
    ];
}
