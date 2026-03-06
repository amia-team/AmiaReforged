using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Actions;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Math;
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

        // Getters
        registry.Register(GetCreatureHPExecutor.CreateDefinition());
        registry.Register(GetPartySizeExecutor.CreateDefinition());
        registry.Register(GetChaosStateExecutor.CreateDefinition());
        registry.Register(GetTimeOfDayExecutor.CreateDefinition());
        registry.Register(GetRandomIntExecutor.CreateDefinition());

        // Math / Logic
        registry.Register(CompareExecutor.CreateDefinition());
        registry.Register(MathOpExecutor.CreateDefinition());
        registry.Register(BooleanOpExecutor.CreateDefinition());
        registry.Register(NotExecutor.CreateDefinition());
    }

    private static List<IGlyphNodeExecutor> CreateExecutors() =>
    [
        // Events
        new BeforeGroupSpawnEventExecutor(),
        new AfterGroupSpawnEventExecutor(),
        new OnCreatureDeathEventExecutor(),

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

        // Getters
        new GetCreatureHPExecutor(),
        new GetPartySizeExecutor(),
        new GetChaosStateExecutor(),
        new GetTimeOfDayExecutor(),
        new GetRandomIntExecutor(),

        // Math / Logic
        new CompareExecutor(),
        new MathOpExecutor(),
        new BooleanOpExecutor(),
        new NotExecutor()
    ];
}
