using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Glyph.Generators;

[Generator]
public sealed class GlyphRegistryGenerator : IIncrementalGenerator
{
    private const string Platform = "AmiaReforged.PwEngine.Features.Glyph.Platform.";
    private const string Runtime = "AmiaReforged.PwEngine.Features.Glyph.Runtime.";
    private static readonly DiagnosticDescriptor Invalid = new("GLYPHGEN001", "Invalid Glyph registration",
        "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Duplicate = new("GLYPHGEN002", "Duplicate Glyph identity",
        "Duplicate {0} '{1}'", "Glyph", DiagnosticSeverity.Error, true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var nodes = context.SyntaxProvider.ForAttributeWithMetadataName(Platform + "GlyphNodeAttribute",
            (n, _) => n is ClassDeclarationSyntax, (cx, _) => Inspect(cx, false));
        var modules = context.SyntaxProvider.ForAttributeWithMetadataName(Platform + "GlyphModuleAttribute",
            (n, _) => n is ClassDeclarationSyntax, (cx, _) => Inspect(cx, true));
        context.RegisterSourceOutput(nodes.Collect().Combine(modules.Collect()),
            (cx, pair) => Emit(cx, pair.Left, pair.Right));
    }

    private sealed class Target
    {
        public string Name = "", Namespace = "", Qualified = "", Error = "";
        public string? RuntimeId;
        public bool Module, Event, Descriptor, Definition, TypeId, BaseNode, Partial, Automatic = true;
        public Location Location = Location.None;
        public readonly List<(string Id, string Member)> Inputs = new();
        public readonly List<string> Exports = new();
    }

    private static Target Inspect(GeneratorAttributeSyntaxContext cx, bool module)
    {
        var symbol = (INamedTypeSymbol)cx.TargetSymbol;
        var syntax = (ClassDeclarationSyntax)cx.TargetNode;
        var target = new Target
        {
            Name = symbol.Name, Namespace = symbol.ContainingNamespace.ToDisplayString(),
            Qualified = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            Module = module, Location = syntax.Identifier.GetLocation(),
            Automatic = !cx.Attributes.Any(a => a.NamedArguments.Any(v => v.Key == "Automatic" && v.Value.Value is false)),
            Event = symbol.GetMembers("Event").OfType<IPropertySymbol>().Any(p => p.IsStatic),
            Descriptor = symbol.GetMembers("Descriptor").OfType<IPropertySymbol>().Any(p => p.IsStatic),
            Definition = symbol.GetMembers("CreateDefinition").Any(), TypeId = symbol.GetMembers("TypeId").Any(),
            Partial = syntax.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
            RuntimeId = symbol.GetMembers("NodeTypeId").OfType<IFieldSymbol>().FirstOrDefault()?.ConstantValue as string
        };
        for (var baseType = symbol.BaseType; baseType != null; baseType = baseType.BaseType)
            if (baseType.ToDisplayString() == Runtime + "GlyphNodeBase") target.BaseNode = true;
        string expected = module ? Platform + "IGlyphModule" : Runtime + "IGlyphNodeExecutor";
        if (symbol.IsAbstract || symbol.ContainingType != null || symbol.TypeParameters.Length != 0 ||
            !symbol.AllInterfaces.Any(i => i.ToDisplayString() == expected) ||
            (target.Automatic || module) && !symbol.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public))
            target.Error = $"{symbol.Name} must be a concrete, non-generic top-level {expected} with a public parameterless constructor. Use a module factory for dependencies.";
        if (target.Descriptor && !target.Partial)
            target.Error = $"{symbol.Name} must be partial to generate descriptor input symbols.";
        if (!target.Descriptor) return target;
        var descriptor = (PropertyDeclarationSyntax)symbol.GetMembers("Descriptor").OfType<IPropertySymbol>()
            .First(p => p.IsStatic).DeclaringSyntaxReferences.First().GetSyntax();
        var semantic = cx.SemanticModel.Compilation.GetSemanticModel(descriptor.SyntaxTree);
        var outputIds = new HashSet<string>(StringComparer.Ordinal);
        var selectedOutputs = new List<string>();
        bool literalResults = false;
        foreach (var assignment in descriptor.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left.ToString() == "TypeId")
                target.RuntimeId = semantic.GetConstantValue(assignment.Right).Value as string;
            if (assignment.Left.ToString() == "Parameters")
            {
                foreach (var expression in assignment.Right.DescendantNodes().OfType<ExpressionSyntax>())
                {
                    ExpressionSyntax? idExpression = expression is InvocationExpressionSyntax invocation
                        ? invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression
                        : expression is AssignmentExpressionSyntax pin && pin.Left.ToString() == "Id" ? pin.Right : null;
                    if (idExpression == null) continue;
                    if (semantic.GetConstantValue(idExpression).Value is string id)
                        target.Inputs.Add((id, string.Concat(id.Split('_').Select(s => s.Length == 0 ? "" : char.ToUpperInvariant(s[0]) + s.Substring(1)))));
                }
            }
            if (assignment.Left.ToString() == "Results")
            {
                literalResults = assignment.Right is CollectionExpressionSyntax;
                foreach (var expression in assignment.Right.DescendantNodes().OfType<ExpressionSyntax>())
                {
                    ExpressionSyntax? idExpression = expression is InvocationExpressionSyntax invocation
                        ? invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression
                        : expression is AssignmentExpressionSyntax pin && pin.Left.ToString() == "Id" ? pin.Right : null;
                    if (idExpression != null && semantic.GetConstantValue(idExpression).Value is string id)
                        outputIds.Add(id);
                }
            }
            if (assignment.Left.ToString() == "Exports")
            {
                foreach (var creation in assignment.Right.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>())
                {
                    if (creation.Ancestors().TakeWhile(n => n != assignment.Right).OfType<BaseObjectCreationExpressionSyntax>().Any()) continue;
                    var first = creation.ArgumentList?.Arguments.FirstOrDefault()?.Expression;
                    if (first != null && semantic.GetConstantValue(first).Value is string name)
                        target.Exports.Add(name);
                    var outputArgument = creation.ArgumentList?.Arguments.FirstOrDefault(a => a.NameColon?.Name.Identifier.ValueText == "OutputPin")
                        ?? creation.ArgumentList?.Arguments.ElementAtOrDefault(1);
                    if (outputArgument != null && (outputArgument.NameColon == null || outputArgument.NameColon.Name.Identifier.ValueText == "OutputPin"))
                        if (semantic.GetConstantValue(outputArgument.Expression).Value is string output)
                            selectedOutputs.Add(output);
                }
            }
        }
        if (literalResults && selectedOutputs.Any(id => !outputIds.Contains(id)))
            target.Error = $"{symbol.Name} exports a missing return output: {string.Join(", ", selectedOutputs.Where(id => !outputIds.Contains(id)))}.";
        if (target.Inputs.Select(p => p.Member).Distinct().Count() != target.Inputs.Count ||
            target.Inputs.Any(p => !SyntaxFacts.IsValidIdentifier(p.Member)))
            target.Error = $"{symbol.Name} has duplicate or invalid parameter identifiers.";
        return target;
    }

    private static void Emit(SourceProductionContext cx, ImmutableArray<Target> nodes, ImmutableArray<Target> modules)
    {
        Target[] all = nodes.Concat(modules).OrderBy(t => t.Qualified, StringComparer.Ordinal).ToArray();
        foreach (var t in all.Where(t => t.Error.Length > 0))
            cx.ReportDiagnostic(Diagnostic.Create(Invalid, t.Location, t.Error));
        foreach (var group in nodes.Where(n => n.RuntimeId != null).GroupBy(n => n.RuntimeId).Where(g => g.Count() > 1))
            foreach (var t in group) cx.ReportDiagnostic(Diagnostic.Create(Duplicate, t.Location, "runtime ID", group.Key));
        foreach (var group in nodes.SelectMany(t => t.Exports.Select(name => (Target: t, Name: name))).GroupBy(x => x.Name).Where(g => g.Count() > 1))
            foreach (var item in group) cx.ReportDiagnostic(Diagnostic.Create(Duplicate, item.Target.Location, "source name", group.Key));

        var output = new StringBuilder("// <auto-generated/>\n#nullable enable\nnamespace " + Platform.TrimEnd('.') + ";\npublic static partial class GlyphGeneratedRegistry\n{\n");
        output.Append("    static partial void RegisterNwn(GlyphModuleBuilder glyph);\n");
        output.Append("    public static System.Collections.Generic.List<" + Runtime + "IGlyphNodeExecutor> CreateExecutors(System.Collections.Generic.IEnumerable<IGlyphModule>? modules = null)\n    {\n        var glyph = new GlyphModuleBuilder();\n        RegisterNwn(glyph);\n");
        foreach (var t in all.Where(t => t.Error.Length == 0 && (t.Module || t.Automatic)))
            output.Append(t.Module ? $"        new {t.Qualified}().Configure(glyph);\n" : $"        glyph.Add<{t.Qualified}>();\n");
        output.Append("        if (modules != null) foreach (var module in modules) module.Configure(glyph);\n        return glyph.Build();\n    }\n");
        output.Append("    public static System.Collections.Generic.IReadOnlyList<GlyphEventDescriptor> CreateEvents() => System.Array.AsReadOnly(new GlyphEventDescriptor[]\n    {\n");
        foreach (var t in all.Where(t => t.Event && t.Error.Length == 0)) output.Append($"        {t.Qualified}.Event,\n");
        output.Append("    });\n}\n");
        cx.AddSource("GlyphGeneratedRegistry.g.cs", output.ToString());
        foreach (var t in nodes.Where(t => t.Descriptor && t.Error.Length == 0))
        {
            var source = new StringBuilder($"// <auto-generated/>\n#nullable enable\nnamespace {t.Namespace};\npublic partial class {t.Name}\n{{\n    public static class Inputs\n    {{\n");
            foreach (var pin in t.Inputs)
                source.Append($"        public const string {pin.Member} = {SymbolDisplay.FormatLiteral(pin.Id, true)};\n");
            source.Append("    }\n");
            if (!t.Definition) source.Append($"    public {(t.BaseNode ? "override " : "")}AmiaReforged.PwEngine.Features.Glyph.Core.GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();\n");
            if (!t.TypeId) source.Append($"    public {(t.BaseNode ? "override " : "")}string TypeId => Descriptor.TypeId;\n");
            source.Append("}\n");
            cx.AddSource(t.Qualified.Replace("global::", "").Replace('.', '_') + ".Inputs.g.cs", source.ToString());
        }
    }
}
