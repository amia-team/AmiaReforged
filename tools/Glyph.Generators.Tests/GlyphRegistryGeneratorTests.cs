using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using Glyph.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Glyph.Generators.Tests;

[TestFixture]
public class GlyphRegistryGeneratorTests
{
    private const string Imports = """
        using System;
        using System.Threading.Tasks;
        using System.Collections.Generic;
        using AmiaReforged.PwEngine.Features.Glyph.Core;
        using AmiaReforged.PwEngine.Features.Glyph.Runtime;
        using AmiaReforged.PwEngine.Features.Glyph.Platform;
        namespace Sample;
        """;

    private static string Node(string name, string runtime, string source) => $$"""
        [GlyphNode]
        public sealed partial class {{name}} : GlyphPureNode
        {
            public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
            {
                TypeId = "{{runtime}}", DisplayName = "Sample", Category = "Sample",
                Parameters = [Pins.InInt("value", "Value", "5")],
                Results = [Pins.Out("result", "Result", GlyphDataType.Int)],
                Exports = [new("{{source}}", "result")]
            };
            protected override async Task<Dictionary<string, object?>> RunPureAsync(GlyphNodeContext cx) =>
                new() { ["result"] = await cx.InInt(Inputs.Value) };
        }
        """;

    private static (GeneratorDriverRunResult Result, Compilation Output) Generate(string source)
    {
        var parse = new CSharpParseOptions(LanguageVersion.CSharp12);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Append(typeof(IGlyphNodeExecutor).Assembly.Location).Distinct()
            .Select(p => MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create("GlyphGeneratorFixture",
            [CSharpSyntaxTree.ParseText(Imports + "\n" + source, parse)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create([new GlyphRegistryGenerator().AsSourceGenerator()], parseOptions: parse);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }

    [Test] public void One_local_declaration_generates_registration_definition_and_input_symbols()
    {
        var (result, output) = Generate(Node("SampleExecutor", "sample.test", "sample_test"));
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
        string generated = string.Join("\n", result.GeneratedTrees.Select(t => t.ToString()));
        Assert.That(generated, Does.Contain("glyph.Add<global::Sample.SampleExecutor>()"));
        Assert.That(generated, Does.Contain("const string Value = \"value\""));
        Assert.That(generated, Does.Contain("CreateDefinition() => Descriptor.CreateDefinition()"));
        Assert.That(generated, Does.Contain("string TypeId => Descriptor.TypeId"));
    }

    [Test] public void Attribute_and_descriptor_can_live_on_different_partial_declarations()
    {
        string source = Node("A", "sample.a", "a").Replace("[GlyphNode]", "") +
            "[GlyphNode] public sealed partial class A { }";
        var (result, output) = Generate(source);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
    }

    [Test] public void Generated_output_is_deterministic_and_sorts_registrations()
    {
        string source = Node("ZExecutor", "sample.z", "z") + Node("AExecutor", "sample.a", "a");
        var first = Generate(source).Result;
        var second = Generate(source).Result;
        Assert.That(first.GeneratedTrees.Select(t => t.ToString()), Is.EqualTo(second.GeneratedTrees.Select(t => t.ToString())));
        string registry = first.GeneratedTrees.Single(t => t.FilePath.EndsWith("GlyphGeneratedRegistry.g.cs")).ToString();
        Assert.That(registry.IndexOf("AExecutor", StringComparison.Ordinal), Is.LessThan(registry.IndexOf("ZExecutor", StringComparison.Ordinal)));
    }

    [TestCase("same.id", "other.id", "same", "same")]
    [TestCase("same.id", "same.id", "first", "second")]
    public void Duplicate_contract_identities_are_compile_time_errors(string aId, string bId, string aName, string bName)
    {
        var result = Generate(Node("A", aId, aName) + Node("B", bId, bName)).Result;
        Assert.That(result.Diagnostics.Any(d => d.Id == "GLYPHGEN002" && d.Severity == DiagnosticSeverity.Error), Is.True);
    }

    [Test] public void Missing_literal_return_output_is_a_compile_time_error()
    {
        var result = Generate(Node("A", "sample.a", "a").Replace("new(\"a\", \"result\")", "new(\"a\", \"missing\")")).Result;
        Assert.That(result.Diagnostics.Any(d => d.Id == "GLYPHGEN001" && d.GetMessage().Contains("missing return output")), Is.True);
    }

    [Test] public void Non_partial_descriptor_has_an_actionable_diagnostic()
    {
        var result = Generate(Node("A", "sample.a", "a").Replace("sealed partial", "sealed")).Result;
        Assert.That(result.Diagnostics.Any(d => d.Id == "GLYPHGEN001" && d.GetMessage().Contains("partial")), Is.True);
    }

    [Test] public void Module_factory_supports_dependency_constructors_without_automatic_instantiation()
    {
        string source = Node("DependencyExecutor", "sample.dependency", "dependency")
            .Replace("[GlyphNode]", "[GlyphNode(Automatic = false)]")
            .Replace("public static GlyphIntrinsicDescriptor", "public DependencyExecutor(string dependency) { }\npublic static GlyphIntrinsicDescriptor");
        source += """
            [GlyphModule]
            public sealed class SampleModule : IGlyphModule
            {
                public void Configure(GlyphModuleBuilder glyph) => glyph.Add(() => new DependencyExecutor("injected"));
            }
            """;
        var (result, output) = Generate(source);
        Assert.That(result.Diagnostics, Is.Empty);
        Assert.That(output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
        string registry = result.GeneratedTrees.Single(t => t.FilePath.EndsWith("GlyphGeneratedRegistry.g.cs")).ToString();
        Assert.That(registry, Does.Contain("SampleModule().Configure(glyph)"));
        Assert.That(registry, Does.Not.Contain("glyph.Add<global::Sample.DependencyExecutor>"));
    }
}
