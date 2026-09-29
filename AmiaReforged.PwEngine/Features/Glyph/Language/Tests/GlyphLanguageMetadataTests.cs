using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.Glyph.Language.Tests;

[TestFixture]
public class GlyphLanguageMetadataTests
{
    private GlyphBootstrap _runtime = null!;
    private GlyphLanguageMetadataDto _metadata = null!;
    [SetUp] public void Setup()
    {
        _runtime = new(new GlyphNodeDefinitionRegistry());
        _metadata = GlyphLanguageMetadata.Create(_runtime.Compiler.Catalog);
    }

    [Test] public void Functions_match_compiler_signatures_defaults_and_restrictions()
    {
        foreach (var symbol in _runtime.Compiler.Catalog.Symbols)
        {
            var function = _metadata.Functions.Single(f => f.Name == symbol.Name);
            Assert.That(function.ReturnType, Is.EqualTo(symbol.ReturnType.Name));
            Assert.That(function.Kind, Is.EqualTo(symbol.Strategy.ToString()));
            Assert.That(function.Description, Is.EqualTo(symbol.Definition.Description));
            Assert.That(function.RestrictToEventType, Is.EqualTo(symbol.Definition.RestrictToEventType?.ToString()));
            Assert.That(function.ScriptCategory, Is.EqualTo(symbol.Definition.ScriptCategory?.ToString()));
            Assert.That(function.AllowedStages, Is.EqualTo(symbol.AllowedStages));
            Assert.That(function.Parameters.Select(p => p.Name), Is.EqualTo(symbol.Parameters.Select(p => p.Id)));
            foreach (var parameter in function.Parameters)
            {
                var pin = symbol.Parameters.Single(p => p.Id == parameter.Name);
                Assert.That(parameter.Type, Is.EqualTo(GlyphTypeSymbol.From(pin.DataType).Name));
                Assert.That(parameter.DefaultValue, Is.EqualTo(pin.DefaultValue));
                Assert.That(parameter.Required, Is.EqualTo(pin.DefaultValue == null));
            }
        }
        Assert.That(_metadata.Functions.Single(f => f.Name == "set_progress").AvailableIn.Select(s => s.Stage),
            Is.EquivalentTo(new[] { "started", "tick" }));
        Assert.That(_metadata.Functions.Single(f => f.Name == "skill_check").Kind, Is.EqualTo("PredicateBranch"));
    }

    [Test] public void Every_advertised_context_field_compiles_in_its_scope()
    {
        foreach (var scope in _metadata.Contexts)
        {
            Assert.That(scope.Fields.Select(f => f.Name).Distinct().Count(), Is.EqualTo(scope.Fields.Count));
            foreach (var field in scope.Fields)
            {
                string body = $"let test_value = {field.Name}";
                if (scope.Stage != null) body = $"{scope.Stage} {{ {body} }}";
                var result = _runtime.Compiler.Compile($"glyph test : {scope.Event} {{ {body} }}");
                Assert.That(result.Success, Is.True, $"{scope.Event}/{scope.Stage}/{field.Name}: {string.Join(", ", result.Diagnostics)}");
            }
        }
        var attempted = _metadata.Contexts.Single(c => c.Event == "interaction" && c.Stage == "attempted");
        Assert.That(attempted.Fields.Any(f => f.Name == "context.session_id"), Is.False);
        var tick = _metadata.Contexts.Single(c => c.Event == "interaction" && c.Stage == "tick");
        Assert.That(tick.Fields.Single(f => f.Name == "progress").Setter, Is.EqualTo("set_progress"));
        Assert.That(tick.Fields.Single(f => f.Name == "context.progress").Setter, Is.Null);
    }

    [Test] public void Receiver_aliases_hide_injected_parameter_and_preserve_binding()
    {
        foreach (var alias in GlyphLanguageAliases.Calls)
        {
            var function = _metadata.Functions.Single(f => f.Name == alias.Name);
            var target = _runtime.Compiler.Catalog.Find(alias.Target)!;
            Assert.That(function.CanonicalName, Is.EqualTo(alias.Target));
            Assert.That(function.ImplicitArgument, Is.EqualTo(alias.ImplicitArgument));
            Assert.That(function.Parameters.Select(p => p.Name), Is.EqualTo(target.Parameters.Skip(1).Select(p => p.Id)));
            foreach (var scope in function.AvailableIn)
            {
                string body = $"if {alias.Name}(\"test\") {{ }}";
                if (scope.Stage != null) body = $"{scope.Stage} {{ {body} }}";
                var result = _runtime.Compiler.Compile($"glyph test : {scope.Event} {{ {body} }}");
                Assert.That(result.Success, Is.True, string.Join(", ", result.Diagnostics));
            }
        }
        Assert.That(_runtime.Compiler.Compile("glyph test : interaction { tick { metadata[\"key\"] = \"value\" let x = metadata[\"key\"] } }").Success, Is.True);
    }

    [Test] public void Receiver_methods_match_the_compiler_catalog()
    {
        var catalog = _runtime.Compiler.Catalog;
        Assert.That(_metadata.ReceiverMethods.Count, Is.EqualTo(catalog.ReceiverMethods.Count));
        foreach (var rm in catalog.ReceiverMethods)
        {
            var meta = _metadata.ReceiverMethods.Single(m => m.Name == rm.Name);
            var target = catalog.Find(rm.Target)!;
            Assert.That(meta.ReceiverType, Is.EqualTo(GlyphTypeSymbol.From(rm.ReceiverType).Name));
            Assert.That(meta.CanonicalName, Is.EqualTo(rm.Target));
            Assert.That(meta.ReturnType, Is.EqualTo(target.ReturnType.Name));
            Assert.That(meta.Description, Is.EqualTo(target.Definition.Description));
            Assert.That(meta.Kind, Is.EqualTo(target.Strategy.ToString()));
            Assert.That(meta.Parameters.Select(p => p.Name), Is.EqualTo(target.Parameters.Skip(1).Select(p => p.Id)));
            foreach (var parameter in meta.Parameters)
            {
                var pin = target.Parameters.Single(p => p.Id == parameter.Name);
                Assert.That(parameter.Type, Is.EqualTo(GlyphTypeSymbol.From(pin.DataType).Name));
                Assert.That(parameter.DefaultValue, Is.EqualTo(pin.DefaultValue));
                Assert.That(parameter.Required, Is.EqualTo(pin.DefaultValue == null));
            }
        }
    }
}
