using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Tests;

/// <summary>
/// Characterization tests for the HasTrait executor. Pure Glyph behavior:
/// reads only the execution context and resolved input, no NWN objects.
/// </summary>
[TestFixture]
public class HasTraitExecutorTests
{
    private static (GlyphNodeInstance node, GlyphExecutionContext ctx) Setup() => (
        new GlyphNodeInstance { TypeId = HasTraitExecutor.NodeTypeId },
        new GlyphExecutionContext
        {
            Graph = new GlyphGraph { Name = "test", EventType = GlyphEventType.BeforeGroupSpawn },
        });

    [Test]
    public async Task ExecuteAsync_WhenTraitListContainsTag_ReturnsTrue()
    {
        HasTraitExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        ctx.Variables["character_traits"] = new List<string> { "brave", "hero" };

        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, id =>
            id == "trait_tag" ? Task.FromResult<object?>("hero") : Task.FromResult<object?>(null));

        ((bool)result.OutputValues["has_trait"]!).Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_WhenTraitTagDiffersOnlyByCase_ReturnsTrue()
    {
        HasTraitExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        ctx.Variables["character_traits"] = new List<string> { "Hero" };

        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, id =>
            id == "trait_tag" ? Task.FromResult<object?>("hero") : Task.FromResult<object?>(null));

        ((bool)result.OutputValues["has_trait"]!).Should().BeTrue();
    }

    [Test]
    public async Task ExecuteAsync_WhenTraitIsAbsent_ReturnsFalse()
    {
        HasTraitExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        ctx.Variables["character_traits"] = new List<string> { "brave", "coward" };

        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, id =>
            id == "trait_tag" ? Task.FromResult<object?>("hero") : Task.FromResult<object?>(null));

        ((bool)result.OutputValues["has_trait"]!).Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenCharacterTraitsVariableIsMissing_ReturnsFalse()
    {
        HasTraitExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();

        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, id =>
            id == "trait_tag" ? Task.FromResult<object?>("hero") : Task.FromResult<object?>(null));

        ((bool)result.OutputValues["has_trait"]!).Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenCharacterTraitsVariableHasWrongType_ReturnsFalse()
    {
        HasTraitExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        ctx.Variables["character_traits"] = "hero";

        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, id =>
            id == "trait_tag" ? Task.FromResult<object?>("hero") : Task.FromResult<object?>(null));

        ((bool)result.OutputValues["has_trait"]!).Should().BeFalse();
    }

    [Test]
    public async Task ExecuteAsync_WhenTraitTagInputIsNull_ReturnsFalse()
    {
        HasTraitExecutor executor = new();
        (GlyphNodeInstance node, GlyphExecutionContext ctx) = Setup();
        ctx.Variables["character_traits"] = new List<string> { "brave", "hero" };

        GlyphNodeResult result = await executor.ExecuteAsync(node, ctx, id =>
            id == "trait_tag" ? Task.FromResult<object?>(null) : Task.FromResult<object?>(null));

        ((bool)result.OutputValues["has_trait"]!).Should().BeFalse();
    }

    [Test]
    public void CreateDefinition_DescribesHasTraitNode()
    {
        HasTraitExecutor executor = new();
        GlyphNodeDefinition def = executor.CreateDefinition();

        def.TypeId.Should().Be(HasTraitExecutor.NodeTypeId);
        def.Category.Should().Be("Traits");
        def.InputPins.Should().ContainSingle(p =>
            p.Id == "trait_tag" && p.DataType == GlyphDataType.String);
        def.OutputPins.Should().ContainSingle(p =>
            p.Id == "has_trait" && p.DataType == GlyphDataType.Bool);
    }
}
