using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using NUnit.Framework;
using FluentAssertions;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

/// <summary>
/// Compiler/language-surface tests for the curated <c>Object.*</c> intrinsics.
/// These verify source spelling, result/parameter types, composition, and the
/// binder's generic argument diagnostics — no running NWN server is required.
/// </summary>
[TestFixture]
public class ObjectApiLanguageTests
{
    private GlyphBootstrap _runtime = null!;

    [SetUp]
    public void Setup() => _runtime = new(new GlyphNodeDefinitionRegistry());

    private GlyphExecutable Compile(string source)
    {
        GlyphCompilationResult result = _runtime.Compiler.Compile(source);
        Assert.That(result.Diagnostics, Is.Empty, string.Join("\n", result.Diagnostics));
        return result.Executable!;
    }

    private const string NearestType = "interaction";

    // ==================== Positive compilation ====================

    [Test]
    public void nearest_object_by_type_compiles()
    {
        Compile($"glyph t : {NearestType} {{ tick {{ let door = Object.nearest_object_by_type(player, \"door\") }} }}");
    }

    [Test]
    public void is_player_compiles()
    {
        Compile($"glyph t : {NearestType} {{ tick {{ if Object.is_player(player) {{ message(player, \"player\") }} }} }}");
    }

    [Theory]
    [TestCase("trigger")]
    [TestCase("door")]
    [TestCase("placeable")]
    [TestCase("creature")]
    [TestCase("waypoint")]
    public void all_five_supported_type_spellings_compile(string type)
    {
        Compile($"glyph t : {NearestType} {{ tick {{ let o = Object.nearest_object_by_type(player, \"{type}\") }} }}");
    }

    [Test]
    public void composition_compiles_and_composes_object_into_is_player()
    {
        Compile("""
            glyph nearby_pc : interaction {
                tick {
                    let nearby = Object.nearest_object_by_type(player, "creature")
                    if Object.is_player(nearby) {
                        message(player, "Nearby PC")
                    }
                }
            }
            """);
    }

    // ==================== Result / parameter types ====================

    [Test]
    public void catalog_exposes_exact_source_spellings_with_expected_signatures()
    {
        GlyphLanguageCatalog catalog = _runtime.Compiler.Catalog;

        GlyphLanguageSymbol nearest = catalog.Find("Object.nearest_object_by_type")
            ?? throw new AssertionError("missing Object.nearest_object_by_type");
        nearest.ReturnType.Name.Should().Be("Object");
        nearest.Parameters.Select(p => p.Id).Should().BeEquivalentTo(new[] { "origin", "type" });
        nearest.Parameters[0].DataType.Should().Be(GlyphDataType.NwObject);
        nearest.Parameters[1].DataType.Should().Be(GlyphDataType.String);

        GlyphLanguageSymbol isPlayer = catalog.Find("Object.is_player")
            ?? throw new AssertionError("missing Object.is_player");
        isPlayer.ReturnType.Name.Should().Be("Bool");
        isPlayer.Parameters.Select(p => p.Id).Should().BeEquivalentTo(new[] { "object" });
        isPlayer.Parameters[0].DataType.Should().Be(GlyphDataType.NwObject);
    }

    [Test]
    public void executors_are_registered_under_stable_runtime_ids()
    {
        _runtime.Compiler.Catalog.Registry.Get("getter.nearest_object_by_type").Should().NotBeNull();
        _runtime.Compiler.Catalog.Registry.Get("getter.is_player").Should().NotBeNull();
    }

    // ==================== Negative compilation (generic binder diagnostics) ====================

    [TestCase("glyph t : interaction { tick { Object.is_player(42) } }", "GLYPH2004")]
    [TestCase("glyph t : interaction { tick { Object.is_player(player, \"extra\") } }", "GLYPH2003")]
    [TestCase("glyph t : interaction { tick { Object.is_player() } }", "GLYPH2003")]
    [TestCase("glyph t : interaction { tick { Object.nearest_object_by_type(player, 42) } }", "GLYPH2004")]
    [TestCase("glyph t : interaction { tick { Object.nearest_object_by_type(42, \"door\") } }", "GLYPH2004")]
    [TestCase("glyph t : interaction { tick { Object.nearest_object_by_type(player, \"door\", \"extra\") } }", "GLYPH2003")]
    [TestCase("glyph t : interaction { tick { Object.nearest_object_by_type(player) } }", "GLYPH2003")]
    public void invalid_argument_usage_is_diagnosed(string source, string code)
    {
        GlyphCompilationResult result = _runtime.Compiler.Compile(source);
        Assert.That(result.Executable, Is.Null);
        Assert.That(result.Diagnostics.Any(d => d.Code == code), Is.True,
            string.Join("\n", result.Diagnostics));
    }

    private sealed class AssertionError : System.Exception { public AssertionError(string m) : base(m) { } }
}
