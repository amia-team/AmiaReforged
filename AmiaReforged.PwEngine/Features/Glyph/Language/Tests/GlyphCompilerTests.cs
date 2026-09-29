using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Compilation;
using AmiaReforged.PwEngine.Features.Glyph.Language.Parsing;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Programs;
using NUnit.Framework;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlyphCompilerTests
{
    private GlyphBootstrap _runtime = null!;
    [SetUp] public void Setup() => _runtime = new(new GlyphNodeDefinitionRegistry());
    private GlyphExecutable Compile(string source)
    {
        var result = _runtime.Compiler.Compile(source);
        Assert.That(result.Diagnostics, Is.Empty, string.Join("\n", result.Diagnostics));
        return result.Executable!;
    }
    private static GlyphExecutionContext Context(GlyphExecutable program) => new()
    {
        Graph = program.CreateExecutionGraph(), EnableTracing = true,
        EncounterContext = new() { AreaResRef = "test", PartySize = 3, GameTime = TimeSpan.FromHours(22) },
        Profile = new() { Name = "test" }, SpawnCount = 1
    };

    [Test]
    public void named_argument_label_that_is_a_keyword_is_accepted_by_the_parser()
    {
        // Regression: the `type` parameter name is lexed as a keyword token, yet the parser must
        // still read it as an identifier-shaped named-argument label rather than failing with
        // GLYPH1001 'Expected identifier'. The argument-name position is keyword-agnostic.
        const string source = "glyph t : interaction { tick { let door = player.get_nearest_object_by_type(type: \"door\") } }";
        var parser = new GlyphParser(new GlyphLexer(source).Lex());
        var unit = parser.Parse();
        Assert.That(unit, Is.Not.Null);
        Assert.That(parser.Diagnostics.Select(d => d.Code), Does.Not.Contain("GLYPH1001"));
    }

    [Test]
    public void excess_keyword_named_argument_reaches_the_binder_not_the_parser()
    {
        // `origin` is supplied by the receiver, so the explicit `origin:` label is excess. This
        // must surface as GLYPH2003 from the binder, which requires the parser to have accepted
        // both keyword-shaped labels without a parse error first.
        const string source = "glyph t : interaction { tick { let x = player.get_nearest_object_by_type(origin: creature, type: \"door\") } }";
        var parser = new GlyphParser(new GlyphLexer(source).Lex());
        parser.Parse();
        Assert.That(parser.Diagnostics.Select(d => d.Code), Does.Not.Contain("GLYPH1001"));
    }
    [Test] public async Task Compiles_branches_math_and_executes_existing_runtime()
    {
        var program = Compile("""
            glyph night : encounter.before_group_spawn {
                let size = party.size
                if time.hour >= 20 || time.hour < 6 { spawn.modify_count(size * 2) }
                else { spawn.modify_count(2) }
            }
            """);
        var context = Context(program);
        Assert.That(await _runtime.Interpreter.ExecuteAsync(context), Is.True);
        Assert.That(context.SpawnCount, Is.EqualTo(6));
        Assert.That(context.TraceLog.Any(t => t.Contains("source.glyph:")), Is.True);
    }
    [Test] public async Task Interaction_stages_remain_independent()
    {
        var program = Compile("glyph prospect : interaction { attempted { fail \"blocked\" } tick { fail \"cancelled\" } }");
        var context = Context(program);
        await _runtime.Interpreter.ExecuteStageAsync(context, "stage.interaction_attempted");
        Assert.That(context.ShouldBlockInteraction, Is.True);
        Assert.That(context.ShouldCancelInteraction, Is.False);
        var tick = Context(program);
        await _runtime.Interpreter.ExecuteStageAsync(tick, "stage.interaction_tick");
        Assert.That(tick.ShouldCancelInteraction, Is.True);
        Assert.That(tick.ShouldBlockInteraction, Is.False);
    }
    [Test] public void Compilation_is_deterministic_and_snapshots_are_isolated()
    {
        const string source = "glyph a : encounter.before_group_spawn { spawn.modify_count(4) }";
        var a = Compile(source); var b = Compile(source);
        Assert.That(GlyphGraphSerializer.Serialize(a.CreateExecutionGraph()), Is.EqualTo(GlyphGraphSerializer.Serialize(b.CreateExecutionGraph())));
        var copy = a.CreateExecutionGraph(); copy.Nodes.Clear(); copy.Edges.Clear();
        Assert.That(a.CreateExecutionGraph().Nodes.Count, Is.GreaterThan(0));
    }
    [TestCase("glyph a : encounter.before_group_spawn { break }", "GLYPH3004")]
    [TestCase("glyph a : encounter.before_group_spawn { heal(true, 4) }", "GLYPH2004")]
    [TestCase("glyph a : encounter.before_group_spawn { heal() }", "GLYPH2003")]
    [TestCase("glyph a : encounter.before_group_spawn { mystery() }", "GLYPH2002")]
    [TestCase("glyph a : encounter.before_group_spawn { let a = creature.hp }", "GLYPH3002")]
    [TestCase("glyph a : trait.on_granted { spawn.modify_count(4) }", "GLYPH3001")]
    [TestCase("glyph a : interaction { attempted { progress = 1 } }", "GLYPH3003")]
    [TestCase("glyph a : interaction { attempted { let a = context.session_id } }", "GLYPH3002")]
    [TestCase("glyph a : interaction { tick { if \"x\" + 2 {} } }", "GLYPH2005")]
    [TestCase("glyph a : interaction { tick { let a = 1 a = 2 } }", "GLYPH2008")]
    [TestCase("glyph a : interaction { tick { set_status(\"Prospecting...\") } }", "GLYPH2004")]
    [TestCase("glyph a : interaction { tick { let a = skill_check(player) } }", "GLYPH2007")]
    public void Invalid_source_never_produces_an_executable(string source, string code)
    {
        var result = _runtime.Compiler.Compile(source);
        Assert.That(result.Executable, Is.Null);
        Assert.That(result.Diagnostics.Any(d => d.Code == code), Is.True, string.Join("\n", result.Diagnostics));
    }
    [Test] public void Lexer_tracks_comments_strings_operators_and_locations()
    {
        var lexer = new GlyphLexer("// note\nlet a = 12.5 >= 3 && !false; \"a\\n\"", "test.glyph");
        var tokens = lexer.Lex();
        Assert.That(lexer.Diagnostics, Is.Empty);
        Assert.That(tokens[0].Span.Line, Is.EqualTo(2));
        Assert.That(tokens[0].Span.Column, Is.EqualTo(1));
        Assert.That(tokens[0].Span.SourceId, Is.EqualTo("test.glyph"));
        Assert.That(tokens.Any(t => t.Value is string s && s == "a\n"), Is.True);
    }
    [TestCase("glyph x : interaction { tick { fail \"unterminated }", "GLYPH1002")]
    [TestCase("glyph x : interaction { tick { @ } }", "GLYPH1005")]
    [TestCase("glyph x interaction {}", "GLYPH1001")]
    public void Syntax_errors_are_diagnostics(string source, string code)
        => Assert.That(_runtime.Compiler.Compile(source).Diagnostics.Any(d => d.Code == code), Is.True);

    [Test] public async Task Activation_preserves_captured_execution_and_rollback_without_compilation()
    {
        var old = Compile("glyph a : encounter.before_group_spawn { spawn.modify_count(2) }");
        var next = Compile("glyph a : encounter.before_group_spawn { spawn.modify_count(5) }");
        Guid id = Guid.NewGuid();
        var first = await _runtime.Programs.ActivateAsync(id, old);
        var captured = _runtime.Programs.GetActive(id)!;
        await _runtime.Programs.ActivateAsync(id, next);
        var oldContext = Context(captured.Executable); var newContext = Context(_runtime.Programs.GetActive(id)!.Executable);
        await _runtime.Interpreter.ExecuteAsync(oldContext); await _runtime.Interpreter.ExecuteAsync(newContext);
        Assert.That(oldContext.SpawnCount, Is.EqualTo(2)); Assert.That(newContext.SpawnCount, Is.EqualTo(5));
        var rolled = await _runtime.Programs.RollbackAsync(id);
        Assert.That(rolled!.Executable, Is.SameAs(first.Executable));
        Assert.That(_runtime.Programs.GetVersions(id).Count, Is.EqualTo(3));
    }
    [Test] public async Task Failed_persistence_does_not_publish_and_readers_do_not_wait()
    {
        var old = Compile("glyph a : interaction {}"); var next = Compile("glyph a : interaction { tick { fail \"x\" } }");
        Guid id = Guid.NewGuid(); await _runtime.Programs.ActivateAsync(id, old);
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task publish = _runtime.Programs.ActivateAsync(id, next, _ => gate.Task);
        Assert.That(_runtime.Programs.GetActive(id)!.Executable, Is.SameAs(old));
        gate.SetException(new IOException("Database unavailable"));
        Assert.ThrowsAsync<IOException>(async () => await publish);
        Assert.That(_runtime.Programs.GetActive(id)!.Executable, Is.SameAs(old));
        Assert.That(_runtime.Programs.GetVersions(id).Count, Is.EqualTo(1));
    }
    [Test] public async Task Low_health_vertical_proof_executes_the_real_getter_branch_and_heal_executor()
    {
        var program = Compile(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "GlyphCorpus", "low_health.glyph")));
        var context = new GlyphExecutionContext
        {
            Graph = program.CreateExecutionGraph(), EnableTracing = true,
            EncounterContext = new() { AreaResRef = "test" }, Profile = new() { Name = "test" },
            SpawnedCreature = NWN.Core.NWScript.OBJECT_INVALID
        };
        await _runtime.Interpreter.ExecuteAsync(context);
        // The invalid-object guards avoid NWN calls; the actual executor chain still runs.
        Assert.That(context.TraceLog.Any(t => t.Contains("Executing node 'action.heal_creature'")), Is.True);
        Assert.That(context.TraceLog.Any(t => t.Contains("ERROR")), Is.False);
    }
    [Test] public void Oversized_alias_expansion_and_nested_syntax_produce_diagnostics()
    {
        string source = "glyph a : encounter.before_group_spawn { let v0 = 1 ";
        for (int i = 1; i < 30; i++) source += $"let v{i} = v{i - 1} + v{i - 1} ";
        source += "spawn.modify_count(v29) }";
        Assert.That(_runtime.Compiler.Compile(source).Diagnostics.Any(d => d.Code == "GLYPH1007"), Is.True);
        string nested = "glyph a : interaction { attempted { " + string.Concat(Enumerable.Repeat("if true {} else ", 150)) + "{} } }";
        Assert.That(_runtime.Compiler.Compile(nested).Diagnostics.Any(d => d.Code == "GLYPH1006"), Is.True);
    }
    [Test] public void All_lexer_operators_have_exact_spans()
    {
        string[] operators = ["{", "}", "(", ")", "[", "]", ",", ".", ":", ";", "=", "==", "!=", "<", "<=", ">", ">=", "+", "-", "*", "/", "%", "!", "&&", "||", "+=", "-="];
        string source = string.Join(" ", operators);
        var lexer = new GlyphLexer(source);
        var tokens = lexer.Lex().Where(t => t.Kind != "eof").ToArray();
        Assert.That(lexer.Diagnostics, Is.Empty);
        Assert.That(tokens.Select(t => t.Kind), Is.EqualTo(operators));
        foreach (var token in tokens) Assert.That(source.Substring(token.Span.Start, token.Span.Length), Is.EqualTo(token.Kind));
    }

    [Test] public async Task Float_literals_execute_independently_of_server_culture()
    {
        var saved = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            var context = Context(Compile("glyph a : encounter.before_group_spawn { spawn.modify_count(1.5 + 1.5) }"));
            await _runtime.Interpreter.ExecuteAsync(context);
            Assert.That(context.SpawnCount, Is.EqualTo(3));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = saved; }
    }

    [Test] public void All_corpus_files_compile()
    {
        string directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "GlyphCorpus");
        Assert.That(Directory.Exists(directory), Is.True);
        foreach (string file in Directory.GetFiles(directory, "*.glyph"))
        {
            var result = _runtime.Compiler.Compile(File.ReadAllText(file), new(file));
            Assert.That(result.Success, Is.True, file + "\n" + string.Join("\n", result.Diagnostics));
        }
    }
}
