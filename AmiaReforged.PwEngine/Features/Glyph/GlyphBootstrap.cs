using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Context;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph;

/// <summary>
/// Bootstraps the Glyph source scripting system at module load.
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
    public Runtime.Programs.GlyphTraceStore Traces { get; } = new();
    public Language.Compilation.GlyphCompiler Compiler { get; }
    public Runtime.Programs.GlyphRuntimeRegistry Programs { get; } = new();

    public GlyphBootstrap(IGlyphNodeDefinitionRegistry registry, IEnumerable<Platform.IGlyphModule>? modules = null)
    {
        Log.Info("Bootstrapping Glyph source scripting system...");

        // Generated stateless modules and normally injected service-backed modules share registration.
        List<IGlyphNodeExecutor> executors = Platform.GlyphGeneratedRegistry.CreateExecutors(modules);

        // Auto-register definitions from each executor (eliminates the old dual-list)
        foreach (IGlyphNodeExecutor executor in executors)
        {
            registry.Register(executor.CreateDefinition());
        }

        Platform.GlyphFeatureVerifier.Verify(registry, executors);

        // Create the interpreter
        Interpreter = new GlyphInterpreter(registry, executors);
        Interpreter.ExecutionCompleted += Traces.Record;
        Compiler = new Language.Compilation.GlyphCompiler(registry);

        Log.Info("Glyph bootstrap complete. {DefCount} definitions registered, {ExecCount} executors loaded " +
                 "(including {CtxCount} context getters).",
            registry.GetAll().Count, executors.Count, executors.Count(e => e is ContextGetterExecutor));
    }

    public void RestorePublished(Persistence.GlyphDefinition definition)
    {
        if (Programs.GetVersions(definition.Id).Count > 0) return;
        try { Persistence.GlyphPublishedVersion.Restore(definition, Compiler, Programs); }
        catch (Exception ex) { Log.Error(ex, "Unable to restore Glyph definition {Id}; active executable retained.", definition.Id); }
    }

    internal static List<IGlyphNodeExecutor> CreateExecutors() => Platform.GlyphGeneratedRegistry.CreateExecutors();
}
