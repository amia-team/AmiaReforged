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

    // ==================== Receiver-method sugar (compiler sugar over the same intrinsics) ===

    [Test]
    public void static_object_api_still_compiles()
    {
        Compile("glyph t : interaction { tick { let door = Object.nearest_object_by_type(player, \"door\"); if Object.is_player(player) { message(player, \"pc\") } } }");
    }

    [Test]
    public void receiver_get_nearest_object_by_type_compiles()
    {
        Compile("glyph t : interaction { tick { let door = player.get_nearest_object_by_type(\"door\") } }");
    }

    [Test]
    public void receiver_is_player_compiles()
    {
        Compile("glyph t : interaction { tick { if player.is_player() { message(player, \"pc\") } } }");
    }

    [Test]
    public void receiver_named_type_argument_compiles()
    {
        Compile("glyph t : interaction { tick { let door = player.get_nearest_object_by_type(type: \"door\") } }");
    }

    [Test]
    public void context_object_receiver_compiles()
    {
        Compile("""
            glyph t : interaction {
                tick {
                    if context.creature.is_player() {
                        message(context.creature.get_nearest_object_by_type("door"), "found")
                    }
                }
            }
            """);
    }

    [Test]
    public void local_object_receiver_compiles()
    {
        Compile("""
            glyph nearby_pc : interaction {
                tick {
                    let nearest = Object.nearest_object_by_type(player, "creature")
                    if nearest.is_player() {
                        message(player, "PC")
                    }
                }
            }
            """);
    }

    [Test]
    public void receiver_returning_receiver_compiles()
    {
        Compile("""
            glyph t : interaction {
                tick {
                    let nearest = player.get_nearest_object_by_type("creature")
                    let door = nearest.get_nearest_object_by_type("door")
                }
            }
            """);
    }

    [Test]
    public void chained_receiver_call_compiles()
    {
        Compile("""
            glyph t : interaction {
                tick {
                    if player.get_nearest_object_by_type("creature").is_player() {
                        message(player, "PC")
                    }
                }
            }
            """);
    }

    [Test]
    public void foreach_object_receiver_compiles()
    {
        // `party.members` is encounter-category, so this runs in an encounter event. The point is
        // that the foreach element is an arbitrary bound Object expression, not a named alias.
        Compile("""
            glyph t : encounter.on_creature_spawn {
                foreach member in party.members {
                    if member.is_player() {
                        damage(member, 1)
                    }
                }
            }
            """);
    }

    [Test]
    public void receiver_and_static_lower_to_same_intrinsic()
    {
        // The `let` value must be used, otherwise an unused lazy binding emits no IR node.
        GlyphExecutable staticExe = Compile("glyph t : interaction { tick { let door = Object.nearest_object_by_type(player, \"door\"); if door.is_player() { message(door, \"found\") } } }");
        GlyphExecutable receiverExe = Compile("glyph t : interaction { tick { let door = player.get_nearest_object_by_type(\"door\"); if door.is_player() { message(door, \"found\") } } }");
        HashSet<string> staticNodes = NodeTypeIds(staticExe);
        HashSet<string> receiverNodes = NodeTypeIds(receiverExe);
        staticNodes.Should().Contain("getter.nearest_object_by_type");
        receiverNodes.Should().Contain("getter.nearest_object_by_type");
        // No duplicate executor: both spellings expand to the identical runtime node set.
        staticNodes.Should().BeEquivalentTo(receiverNodes);
    }

    [Test]
    public void is_player_receiver_and_static_lower_to_same_intrinsic()
    {
        GlyphExecutable staticExe = Compile("glyph t : interaction { tick { if Object.is_player(player) { message(player, \"pc\") } } }");
        GlyphExecutable receiverExe = Compile("glyph t : interaction { tick { if player.is_player() { message(player, \"pc\") } } }");
        NodeTypeIds(staticExe).Should().Contain("getter.is_player");
        NodeTypeIds(receiverExe).Should().Contain("getter.is_player");
    }

    [Theory]
    [TestCase("glyph t : interaction { tick { let x = 42.is_player() } }", "GLYPH2004")]
    [TestCase("glyph t : interaction { tick { let x = \"hello\".is_player() } }", "GLYPH2004")]
    [TestCase("glyph t : interaction { tick { let x = player.get_nearest_object_by_type(123) } }", "GLYPH2004")]
    [TestCase("glyph t : encounter.before_group_spawn { let x = party.size.is_player() }", "GLYPH2004")]
    [TestCase("glyph t : interaction { tick { let x = player.is_player(player) } }", "GLYPH2003")]
    [TestCase("glyph t : interaction { tick { let x = player.get_nearest_object_by_type() } }", "GLYPH2003")]
    [TestCase("glyph t : interaction { tick { let x = player.get_nearest_object_by_type(origin: creature, type: \"door\") } }", "GLYPH2003")]
    public void receiver_method_violations_are_diagnosed(string source, string code)
    {
        GlyphCompilationResult result = _runtime.Compiler.Compile(source);
        Assert.That(result.Executable, Is.Null);
        Assert.That(result.Diagnostics.Any(d => d.Code == code), Is.True,
            string.Join("\n", result.Diagnostics));
    }

    private HashSet<string> NodeTypeIds(GlyphExecutable exe) =>
        exe.CreateExecutionGraph().Nodes.Select(n => n.TypeId).ToHashSet();
}
