using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using Glyph.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
namespace Glyph.Generators.Tests;

[TestFixture]
public sealed class NwnBindingGeneratorTests
{
    private sealed class Manifest(string text) : AdditionalText
    {
        public override string Path => "fixture.nwnbindings";
        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }
    private static string Function(string method, string name, string mode = "pure", string result = "", string rules = "", string receiver = "", string aliases = "", string adapter = "") =>
        string.Join("|", "function", method, name, mode, result, rules, receiver, aliases, "Fixture", adapter, "Fixture documentation", "");
    private static (GeneratorDriverRunResult Result, Compilation Output) Generate(string api, string manifest, string extra = "")
    {
        var parse = new CSharpParseOptions(LanguageVersion.CSharp12);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(System.IO.Path.PathSeparator)
            .Append(typeof(IGlyphNodeExecutor).Assembly.Location).Distinct().Select(p => MetadataReference.CreateFromFile(p));
        string source = "namespace NWN.Core { public static class NWScript { public const uint OBJECT_INVALID = 2130706432u; " + api + " } }\n" + extra;
        var compilation = CSharpCompilation.Create("NwnFixture", [CSharpSyntaxTree.ParseText(source, parse)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new GlyphRegistryGenerator().AsSourceGenerator(), new NwnBindingGenerator().AsSourceGenerator()],
            [new Manifest(manifest)], parse);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }
    private static string Text(GeneratorDriverRunResult result) => string.Join("\n", result.GeneratedTrees.Select(t => t.ToString()));
    private static void Valid((GeneratorDriverRunResult Result, Compilation Output) fixture)
    {
        Assert.That(fixture.Result.Diagnostics, Is.Empty);
        Assert.That(fixture.Output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
    }

    [Test] public void Pure_query_has_direct_dispatch_object_conversion_and_no_receiver()
    {
        var fixture = Generate("public static string GetTag(uint oObject) => \"tag\";", Function("GetTag", "nwn.get_tag"));
        Valid(fixture);
        string text = Text(fixture.Result);
        Assert.That(text, Does.Contain("NWScript.GetTag(p0)").And.Contain("cx.InObject(\"object\")").And.Not.Contain("ReceiverMethods:"));
        Assert.That(text, Does.Not.Contain("Reflection").And.Not.Contain("Invoke("));
    }
    [Test] public void Action_and_impure_object_result_have_exec_flow()
    {
        var fixture = Generate("public static uint CreateObject(int nType) => 1u; public static void SetLocalInt(uint oObject, string sName, int nValue) {}",
            Function("CreateObject", "nwn.create_object", "action") + "\n" + Function("SetLocalInt", "nwn.set_local_int", "action"));
        Valid(fixture);
        string text = Text(fixture.Result);
        Assert.That(text, Does.Contain("Archetype = GlyphNodeArchetype.Action").And.Contain("NextExecPinId = \"exec_out\"").And.Contain("GlyphNwnValue.NormalizeObject(value)"));
    }
    [Test] public void Sentinel_booleans_are_typed_and_converted_in_both_directions()
    {
        var fixture = Generate("public static int GetIsOpen(uint oObject, int bCheck = 1) => bCheck;", Function("GetIsOpen", "nwn.is_open", result: "Bool", rules: "bCheck:Bool"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("cx.InBool(\"check\") ? 1 : 0").And.Contain("value != 0").And.Contain("GlyphDataType.Bool, \"true\""));
    }
    [Test] public void Locations_and_effects_keep_distinct_semantic_types()
    {
        var fixture = Generate("public static System.IntPtr GetLocation(uint oObject) => default; public static System.IntPtr EffectLinkEffects(System.IntPtr eFirst, System.IntPtr eSecond) => default; public static void JumpToLocation(System.IntPtr lTarget) {}",
            Function("GetLocation", "nwn.get_location", result: "Location") + "\n" + Function("EffectLinkEffects", "nwn.effect_link_effects", result: "Effect", rules: "eFirst:Effect,eSecond:Effect") + "\n" + Function("JumpToLocation", "nwn.jump", "action", rules: "lTarget:Location"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("new GlyphNwnLocation(value)").And.Contain("new GlyphNwnEffect(value)").And.Contain("cx.In<GlyphNwnLocation>").And.Contain("cx.In<GlyphNwnEffect>"));
    }
    [Test] public void Command_injects_an_explicit_actor_and_never_exposes_a_delegate_pin()
    {
        var fixture = Generate("public static void AssignCommand(uint oActor, System.Action callback) => callback(); public static void ActionAttack(uint oTarget) {}",
            Function("ActionAttack", "nwn.action_attack", "command"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("Pins.InObject(\"actor\", \"Actor\")").And.Contain("NWScript.AssignCommand(actor, () => NWScript.ActionAttack(p0))").And.Contain("Requires Glyph-native scheduling"));
    }
    [Test] public void Constants_are_generated_in_longest_matching_domain_and_include_object_semantics()
    {
        var fixture = Generate("public const int OBJECT_TYPE_CREATURE = 1; public const int VFX_DUR_AURA = 42;",
            "domain|OBJECT|OBJECT\ndomain|OBJECT_TYPE|OBJECT_TYPE\ndomain|VFX|VFX");
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("new(\"OBJECT_TYPE.CREATURE\", \"OBJECT_TYPE\", \"Int\", 1").And.Contain("new(\"OBJECT.INVALID\", \"OBJECT\", \"Object\", 2130706432u").And.Contain("VFX.DUR_AURA"));
    }
    [Test] public void Constant_literals_handle_booleans_and_numeric_native_suffixes()
    {
        var fixture = Generate("public const bool FLAG_ENABLED = true; public const int DAMAGE_BONUS_1 = 1;", "domain|FLAG|FLAG\ndomain|DAMAGE_BONUS|DAMAGE_BONUS");
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("\"Bool\", true").And.Contain("DAMAGE_BONUS.VALUE_1"));
    }
    [Test] public void Native_OBJECT_SELF_defaults_require_an_explicit_Glyph_object()
    {
        var fixture = Generate("public static string GetTag(uint oObject = 0) => \"\";", Function("GetTag", "nwn.get_tag"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("GlyphDataType.NwObject, null"));
    }
    [Test] public void Procedural_subject_preserves_the_required_pin_after_receiver_removal()
    {
        var fixture = Generate("public static int GetGold(uint oTarget = 2130706432u) => 0;", Function("GetGold", "nwn.get_gold"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("GlyphDataType.NwObject, null"));
    }

    [Test] public void Omitted_optional_parameters_keep_the_native_default()
    {
        var fixture = Generate("public static int GetValue(int nMode = 7) => nMode;", Function("GetValue", "nwn.get_value", rules: "nMode:omit"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("NWScript.GetValue((int)7)"));
    }
    [Test] public void Manual_override_registers_only_its_existing_executor()
    {
        const string adapter = """
            namespace Fixture {
            [AmiaReforged.PwEngine.Features.Glyph.Platform.GlyphNode]
            public partial class Manual : AmiaReforged.PwEngine.Features.Glyph.Runtime.GlyphPureNode {
              public static AmiaReforged.PwEngine.Features.Glyph.Platform.GlyphIntrinsicDescriptor Descriptor { get; } = new() {
                TypeId = "old.tag", DisplayName = "Tag", Category = "Object", Exports = [new("nwn.get_tag", "value")],
                Results = [AmiaReforged.PwEngine.Features.Glyph.Core.Pins.Out("value", "Value", AmiaReforged.PwEngine.Features.Glyph.Core.GlyphDataType.String)] };
              protected override System.Threading.Tasks.Task<System.Collections.Generic.Dictionary<string, object?>> RunPureAsync(AmiaReforged.PwEngine.Features.Glyph.Runtime.GlyphNodeContext cx) => System.Threading.Tasks.Task.FromResult(new System.Collections.Generic.Dictionary<string, object?>());
            } }
            """;
        var fixture = Generate("public static string GetTag(uint oObject) => \"\";", Function("GetTag", "nwn.get_tag", "manual", adapter: "Fixture.Manual"), adapter);
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("glyph.Add<global::Fixture.Manual>()").And.Not.Contain("class NwnGetTagExecutor"));
    }
    [Test] public void Exclusions_and_callbacks_have_reasons_and_no_wrappers()
    {
        var fixture = Generate("public static void DelayCommand(float fSeconds, System.Action callback) {} public static void ExecuteScript(string sScript) {}",
            Function("ExecuteScript", "", "exclude"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("\"deferred\"").And.Contain("\"excluded\"").And.Not.Contain("class NwnDelayCommandExecutor").And.Not.Contain("class NwnExecuteScriptExecutor"));
    }
    [TestCase("public static int A(int x) => x; public static int B(int x) => x;", "function|A|nwn.a|pure|||||Test|||\nfunction|B|nwn.a|pure|||||Test|||", "GLYPHNW001")]
    [TestCase("public static void A(System.Action callback) {}", "function|A|nwn.a|action|||||Test|||", "GLYPHNW002")]
    [TestCase("public static System.IntPtr A() => default;", "function|A|nwn.a|pure|||||Test|||", "GLYPHNW003")]
    [TestCase("public static int A(int x) => x;", "function|A|nwn.a|pure||x:Object|||Test|||", "GLYPHNW004")]
    [TestCase("public static int A(System.IntPtr l) => 0; public static int B(System.IntPtr l) => 0;", "function|A|nwn.a|pure||l:Location|language_value:same||Test|||\nfunction|B|nwn.b|pure||l:Location|language_value:same||Test|||", "GLYPHNW005")]
    [TestCase("public const int FOO_X = 1; public const int BAR_X = 2;", "domain|FOO|X\ndomain|BAR|X", "GLYPHNW006")]
    [TestCase("public static int A() => 0;", "function|A|nwn.a|manual|||||Test|Missing.Adapter||", "GLYPHNW007")]
    [TestCase("public static int A() => 0;", "function|Missing|nwn.a|pure|||||Test|||", "GLYPHNW008")]
    public void Malformed_bindings_have_specific_diagnostics(string api, string manifest, string code) =>
        Assert.That(Generate(api, manifest).Result.Diagnostics.Select(d => d.Id), Does.Contain(code));

    [TestCase("get_tag")]
    [TestCase("language_value:get_tag")]
    [TestCase("domain_abstraction:get_tag")]
    [TestCase("legacy:get_tag")]
    public void Raw_object_receiver_projection_is_rejected(string receiver)
    {
        var fixture = Generate("public static string GetTag(uint oObject) => \"tag\";",
            Function("GetTag", "nwn.get_tag", receiver: receiver));
        Assert.That(fixture.Result.Diagnostics.Select(d => d.Id), Does.Contain("GLYPHNW009"));
    }

    [Test] public void Deliberate_typed_value_receiver_has_explicit_policy()
    {
        var fixture = Generate("public static float GetFacingFromLocation(System.IntPtr lLocation) => 0;",
            Function("GetFacingFromLocation", "nwn.get_facing_from_location", rules: "lLocation:Location", receiver: "language_value:get_facing"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("ReceiverType: GlyphDataType.Location, ReceiverPolicy: GlyphReceiverPolicy.LanguageValue"));
    }

    [Test] public void Command_cannot_claim_a_typed_value_receiver()
    {
        var fixture = Generate("public static void AssignCommand(uint actor, System.Action callback) {} public static void ActionMove(System.IntPtr lLocation) {}",
            Function("ActionMove", "nwn.action_move", "command", rules: "lLocation:Location", receiver: "language_value:move"));
        Assert.That(fixture.Result.Diagnostics.Select(d => d.Id), Does.Contain("GLYPHNW009"));
    }

    [Test] public void Manual_receiver_must_be_classified_on_its_descriptor()
    {
        var fixture = Generate("public static string GetTag(uint oObject) => \"tag\";",
            Function("GetTag", "nwn.get_tag", "manual", receiver: "language_value:get_tag", adapter: "Missing.Adapter"));
        Assert.That(fixture.Result.Diagnostics.Select(d => d.Id), Does.Contain("GLYPHNW009"));
    }

    [Test] public void Generation_is_deterministic()
    {
        string manifest = Function("GetTag", "nwn.get_tag");
        Assert.That(Text(Generate("public static string GetTag(uint oObject) => \"\";", manifest).Result),
            Is.EqualTo(Text(Generate("public static string GetTag(uint oObject) => \"\";", manifest).Result)));
    }
    [Test] public void Documentation_parameter_provenance_uses_native_symbols_after_pin_renaming()
    {
        var fixture = Generate("public static int GetAbilityScore(uint oCreature, int nAbilityType, int nBaseAbilityScore = 0) => 0;",
            Function("GetAbilityScore", "nwn.get_ability_score", rules: "nBaseAbilityScore:Bool:base_score"));
        Valid(fixture);
        Assert.That(Text(fixture.Result), Does.Contain("SourceParameter = \"oCreature\"")
            .And.Contain("SourceParameter = \"nBaseAbilityScore\"").And.Contain("Pins.In(\"base_score\""));
    }

}
