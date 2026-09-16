using AmiaReforged.PwEngine.Features.Glyph;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// Assembly-wide invariants for every node executor with a parameterless constructor
/// (parameterised executors such as ContextGetterExecutor are built by bootstrap
/// and covered by their own tests). Catches TypeId collisions, definition/executor
/// drift, and duplicate pin IDs without needing a server.
/// </summary>
[TestFixture]
public class GlyphRegistryInvariantTests
{
    private static IEnumerable<IGlyphNodeExecutor> AllExecutors() =>
        typeof(IGlyphNodeExecutor).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IGlyphNodeExecutor).IsAssignableFrom(t))
            .Where(t => t.GetConstructor(Type.EmptyTypes) != null)
            // Test stubs (e.g. StubEntryExecutor) deliberately reuse production TypeIds.
            .Where(t => t.Namespace == null || !t.Namespace.Contains(".Tests", StringComparison.Ordinal))
            .Select(t => (IGlyphNodeExecutor)Activator.CreateInstance(t)!);

    [Test]
    public void All_type_ids_are_unique()
    {
        string[] typeIds = AllExecutors().Select(e => e.TypeId).ToArray();
        typeIds.Should().NotBeEmpty();
        typeIds.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void Every_definition_type_id_matches_its_executor()
    {
        foreach (IGlyphNodeExecutor executor in AllExecutors())
        {
            GlyphNodeDefinition def = executor.CreateDefinition();
            def.TypeId.Should().Be(executor.TypeId,
                $"definition for {executor.GetType().Name} must match the executor TypeId");
        }
    }

    [Test]
    public void Pin_ids_are_unique_within_each_node()
    {
        foreach (IGlyphNodeExecutor executor in AllExecutors())
        {
            GlyphNodeDefinition def = executor.CreateDefinition();
            def.InputPins.Select(p => p.Id).Should().OnlyHaveUniqueItems($"{executor.TypeId} input pins");
            def.OutputPins.Select(p => p.Id).Should().OnlyHaveUniqueItems($"{executor.TypeId} output pins");
            def.InputPins.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Id));
            def.OutputPins.Should().OnlyContain(p => !string.IsNullOrWhiteSpace(p.Id));
        }
    }

    [Test]
    public void Every_bootstrap_executor_type_id_resolves_in_registry()
    {
        GlyphNodeDefinitionRegistry registry = new();
        _ = new GlyphBootstrap(registry);

        IReadOnlyList<GlyphNodeDefinition> definitions = registry.GetAll();
        definitions.Should().NotBeEmpty();
        definitions.Select(d => d.TypeId).Should().OnlyHaveUniqueItems();

        foreach (GlyphNodeDefinition definition in definitions)
        {
            registry.Get(definition.TypeId).Should().NotBeNull(
                $"TypeId '{definition.TypeId}' must resolve in the registry");
        }
    }
}
