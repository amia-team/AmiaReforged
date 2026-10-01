using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Glyph.Generators;

/// <summary>Builds an explicit NWN surface from the referenced assembly and reviewed manifest.</summary>
[Generator]
public sealed class NwnBindingGenerator : IIncrementalGenerator
{
    private const string Platform = "AmiaReforged.PwEngine.Features.Glyph.Platform";
    private const string Nwn = "AmiaReforged.PwEngine.Features.Glyph.Nwn";
    private static readonly DiagnosticDescriptor Error1 = new("GLYPHNW001", "Duplicate NWN binding", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error2 = new("GLYPHNW002", "Unsupported NWN parameter", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error3 = new("GLYPHNW003", "Unsupported NWN return", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error4 = new("GLYPHNW004", "Invalid semantic mapping", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error5 = new("GLYPHNW005", "Duplicate NWN receiver", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error6 = new("GLYPHNW006", "Constant collision", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error7 = new("GLYPHNW007", "Missing manual adapter", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error8 = new("GLYPHNW008", "Invalid NWN manifest", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor Error9 = new("GLYPHNW009", "Invalid NWN receiver policy", "{0}", "Glyph", DiagnosticSeverity.Error, true);
    private static readonly DiagnosticDescriptor[] Errors = { Error1, Error2, Error3, Error4, Error5, Error6, Error7, Error8, Error9 };

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var manifests = context.AdditionalTextsProvider.Where(f => f.Path.EndsWith(".nwnbindings", StringComparison.Ordinal))
            .Select((file, token) => file.GetText(token)?.ToString() ?? "").Collect();
        context.RegisterSourceOutput(context.CompilationProvider.Combine(manifests), (cx, input) =>
        {
            if (input.Right.Length == 0) return;
            Emit(cx, input.Left, string.Join("\n", input.Right));
        });
    }

    private sealed class Entry
    {
        public string Method = "", Name = "", Mode = "", Return = "", Rules = "", Receiver = "", Aliases = "", Group = "", Adapter = "", Description = "", Deprecated = "";
    }
    private sealed class Parameter
    {
        public IParameterSymbol Symbol = null!;
        public string Type = "", Pin = "", Default = "null";
        public bool Omit;
    }
    private static bool Add<T>(Dictionary<string, T> dictionary, string key, T value)
    {
        if (dictionary.ContainsKey(key)) return false;
        dictionary.Add(key, value);
        return true;
    }
    private static string Literal(string value) => SymbolDisplay.FormatLiteral(value, true);
    internal static string Snake(string name)
    {
        name = Regex.Replace(name, "([A-Z]+)([A-Z][a-z])", "$1_$2");
        return Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();
    }
    private static string DefaultType(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_Void => "Void", SpecialType.System_Boolean => "Bool",
        SpecialType.System_Int32 => "Int", SpecialType.System_Single or SpecialType.System_Double => "Float",
        SpecialType.System_String => "String", SpecialType.System_UInt32 => "Object",
        _ => ""
    };
    private static string RuntimeType(string type) => type == "Object" ? "NwObject" : type;
    private static bool Compatible(ITypeSymbol clr, string type) => type switch
    {
        "Bool" => clr.SpecialType is SpecialType.System_Boolean or SpecialType.System_Int32,
        "Object" => clr.SpecialType == SpecialType.System_UInt32,
        "Location" or "Effect" => clr.SpecialType == SpecialType.System_IntPtr,
        _ => DefaultType(clr) == type && type.Length > 0
    };
    private static string Read(Parameter p, string variable)
    {
        string id = Literal(p.Pin);
        string read = p.Type switch
        {
            "Object" => $"await cx.InObject({id})", "Bool" => $"await cx.InBool({id})",
            "Float" => $"await cx.InFloat({id})", "Int" => $"await cx.InInt({id})",
            "String" => $"await cx.InString({id})", "Location" => $"(await cx.In<GlyphNwnLocation>({id})).Handle",
            "Effect" => $"(await cx.In<GlyphNwnEffect>({id})).Handle", _ => ""
        };
        if (p.Type == "Bool" && p.Symbol.Type.SpecialType == SpecialType.System_Int32) read = "(" + read + " ? 1 : 0)";
        if (p.Type == "Float" && p.Symbol.Type.SpecialType == SpecialType.System_Single) read = "(float)(" + read + ")";
        return $"        var {variable} = {read};\n";
    }
    private static string ConvertReturn(string type, string value, ITypeSymbol clr) => type switch
    {
        "Object" => $"GlyphNwnValue.NormalizeObject({value})", "Bool" when clr.SpecialType == SpecialType.System_Int32 => $"{value} != 0",
        "Location" => $"new GlyphNwnLocation({value})", "Effect" => $"new GlyphNwnEffect({value})",
        "Float" => $"(double){value}", _ => value
    };
    private static string NativeDefault(IParameterSymbol parameter)
    {
        if (!parameter.HasExplicitDefaultValue || parameter.ExplicitDefaultValue == null) return "default";
        string value = Convert.ToString(parameter.ExplicitDefaultValue, CultureInfo.InvariantCulture) ?? "";
        return parameter.Type.SpecialType switch
        {
            SpecialType.System_String => Literal(value),
            SpecialType.System_Boolean => (bool)parameter.ExplicitDefaultValue ? "true" : "false",
            SpecialType.System_Single => value + "f",
            SpecialType.System_Double => value + "d",
            SpecialType.System_UInt32 => value + "u",
            _ => "(" + parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ")" + value
        };
    }
    private static string Documentation(IMethodSymbol method)
    {
        try
        {
            string? xml = method.GetDocumentationCommentXml();
            if (string.IsNullOrWhiteSpace(xml)) return "NWScript." + method.Name + ".";
            return Regex.Replace(XDocument.Parse(xml).Descendants("summary").FirstOrDefault()?.Value ?? "", @"\s+", " ").Trim();
        }
        catch { return "NWScript." + method.Name + "."; }
    }
    private static string Signature(IMethodSymbol method) => method.ReturnType.ToDisplayString() + " " + method.Name + "(" +
        string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString() + " " + p.Name +
            (p.HasExplicitDefaultValue ? " = " + Convert.ToString(p.ExplicitDefaultValue, CultureInfo.InvariantCulture) : ""))) + ")";

    private static void Emit(SourceProductionContext cx, Compilation compilation, string manifest)
    {
        void Error(int code, string message) => cx.ReportDiagnostic(Diagnostic.Create(Errors[code - 1], Location.None, message));
        var api = compilation.GetTypeByMetadataName("NWN.Core.NWScript");
        if (api == null) { Error(8, "NWN.Core.NWScript is missing from compilation references."); return; }
        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        var domains = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in manifest.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#", StringComparison.Ordinal)) continue;
            var cells = line.TrimEnd('\r').Split('|').Select(c => c.Trim()).ToArray();
            if (cells[0] == "domain" && cells.Length == 3)
            {
                if (!Add(domains, cells[1], cells[2])) Error(6, "Duplicate constant domain " + cells[1]);
                continue;
            }
            if (cells.Length != 12 || cells[0] != "function") { Error(8, "Expected function plus 11 fields or domain plus 2 fields: " + line); continue; }
            var e = new Entry { Method = cells[1], Name = cells[2], Mode = cells[3], Return = cells[4], Rules = cells[5], Receiver = cells[6], Aliases = cells[7], Group = cells[8], Adapter = cells[9], Description = cells[10], Deprecated = cells[11] };
            if (e.Receiver.Length > 0)
            {
                var receiver = e.Receiver.Split(':');
                if (receiver.Length != 2 || receiver[0] != "language_value" || receiver[1].Length == 0)
                {
                    Error(9, "NWN receivers require explicit language_value:name policy; domain and legacy receivers must be hand-authored: " + e.Method);
                    continue;
                }
                e.Receiver = receiver[1];
            }
            if (!Add(entries, e.Method, e)) Error(8, "Duplicate method entry " + e.Method);
            if (e.Mode is not ("pure" or "action" or "command" or "manual" or "exclude" or "deferred")) Error(8, "Unknown mode for " + e.Method);
        }
        var methods = api.GetMembers().OfType<IMethodSymbol>().Where(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public && m.MethodKind == MethodKind.Ordinary)
            .OrderBy(m => m.Name, StringComparer.Ordinal).ThenBy(Signature, StringComparer.Ordinal).ToArray();
        foreach (var entry in entries.Values)
            if (!methods.Any(m => m.Name == entry.Method)) Error(8, "Manifest method no longer exists: " + entry.Method);
        var sourceNames = new HashSet<string>(StringComparer.Ordinal);
        var receivers = new HashSet<string>(StringComparer.Ordinal);
        var registrations = new StringBuilder();
        var wrappers = new StringBuilder("// <auto-generated/>\n#nullable enable\nusing System;\nusing System.Collections.Generic;\nusing System.Threading.Tasks;\nusing NWN.Core;\nusing AmiaReforged.PwEngine.Features.Glyph.Core;\nusing AmiaReforged.PwEngine.Features.Glyph.Runtime;\nusing AmiaReforged.PwEngine.Features.Glyph.Platform;\nnamespace " + Nwn + ";\n");
        var coverage = new StringBuilder();
        foreach (var method in methods)
        {
            if (!entries.TryGetValue(method.Name, out var e))
            {
                bool callbacks = method.Parameters.Any(p => p.Type.TypeKind == TypeKind.Delegate);
                bool unsupported = DefaultType(method.ReturnType).Length == 0 || method.Parameters.Any(p => DefaultType(p.Type).Length == 0 || p.RefKind != RefKind.None);
                coverage.Append($"        new({Literal(method.Name)}, {Literal(Signature(method))}, {Literal(callbacks ? "deferred" : unsupported ? "unsupported" : "excluded")}, null, {Literal(callbacks ? "Requires Glyph-native scheduling/control flow; delegates are not Glyph values." : unsupported ? "Signature needs an explicit semantic adapter/type mapping." : "Not selected by the reviewed manifest; review before publishing.")}),\n");
                continue;
            }
            coverage.Append($"        new({Literal(method.Name)}, {Literal(Signature(method))}, {Literal(e.Mode == "manual" || e.Mode == "command" ? "adapted" : e.Mode is "pure" or "action" ? "bound" : e.Mode == "exclude" ? "excluded" : e.Mode)}, {(e.Name.Length == 0 ? "null" : Literal(e.Name))}, {Literal(e.Description)}),\n");
            if (e.Mode is "exclude" or "deferred") continue;
            foreach (string name in new[] { e.Name }.Concat(e.Aliases.Split(',').Where(s => s.Length > 0)))
                if (!sourceNames.Add(name)) Error(1, "Duplicate Glyph NWN name " + name);

            if (e.Mode == "manual")
            {
                if (e.Receiver.Length > 0) Error(9, "Manual receivers must be classified on the authoritative adapter descriptor, not the manifest: " + e.Method);
                var adapter = compilation.GetTypeByMetadataName(e.Adapter);
                if (adapter == null || !adapter.GetMembers("Descriptor").OfType<IPropertySymbol>().Any(p => p.IsStatic)) Error(7, "Missing adapter descriptor: " + e.Adapter);
                continue;
            }
            if (methods.Count(m => m.Name == method.Name) != 1) { Error(8, "Overloaded method requires an adapter: " + method.Name); continue; }
            string resultType = e.Return.Length == 0 ? DefaultType(method.ReturnType) : e.Return;
            if (!Compatible(method.ReturnType, resultType)) { Error(3, "Unsupported return mapping for " + method.Name + ": " + method.ReturnType + " -> " + resultType); continue; }
            if ((e.Mode == "pure" && resultType == "Void") || (e.Mode == "command" && resultType != "Void")) { Error(8, "Invalid execution mode for " + method.Name); continue; }
            var rules = new Dictionary<string, string[]>(StringComparer.Ordinal);
            foreach (string rule in e.Rules.Split(','))
            {
                if (rule.Length == 0) continue;
                var parts = rule.Split(':');
                if (!method.Parameters.Any(p => p.Name == parts[0]) || !Add(rules, parts[0], parts.Skip(1).ToArray())) Error(8, "Unknown/duplicate parameter rule " + method.Name + "." + parts[0]);
            }
            var parameters = new List<Parameter>();
            bool invalid = false;
            foreach (var p in method.Parameters)
            {
                rules.TryGetValue(p.Name, out var rule);
                string type = rule?.ElementAtOrDefault(0) ?? "";
                if (type.Length == 0) type = DefaultType(p.Type);
                string pin = rule?.ElementAtOrDefault(1) ?? "";
                if (pin.Length == 0) pin = Snake(p.Name.Length > 1 && "nfosbelv".Contains(p.Name[0]) && char.IsUpper(p.Name[1]) ? p.Name.Substring(1) : p.Name);
                var parameter = new Parameter { Symbol = p, Type = type, Pin = pin, Omit = type == "omit" };
                if (parameter.Omit)
                {
                    if (!p.IsOptional) { Error(2, "Only optional parameters can be omitted: " + method.Name + "." + p.Name); invalid = true; }
                }
                else if (p.RefKind != RefKind.None || !Compatible(p.Type, type))
                { Error(type == "Object" ? 4 : 2, "Unsupported parameter mapping " + method.Name + "." + p.Name + ": " + p.Type + " -> " + type); invalid = true; }
                if (p.HasExplicitDefaultValue && p.ExplicitDefaultValue != null)
                {
                    string value = type == "Bool" ? (Convert.ToInt32(p.ExplicitDefaultValue, CultureInfo.InvariantCulture) != 0 ? "true" : "false") : Convert.ToString(p.ExplicitDefaultValue, CultureInfo.InvariantCulture) ?? "";
                    // Native object defaults depend on OBJECT_SELF. Make receiver/subject parameters explicit.
                    parameter.Default = type is "Location" or "Effect" ? Literal("invalid") : Literal(value);
                    if (type == "Object" && Convert.ToUInt32(p.ExplicitDefaultValue, CultureInfo.InvariantCulture) == 0)
                        parameter.Default = "null";
                }
                parameters.Add(parameter);
            }
            if (parameters.Where(p => !p.Omit).GroupBy(p => p.Pin).Any(g => g.Count() > 1)) { Error(8, "Duplicate pin ID in " + method.Name); invalid = true; }
            // Procedural object subjects remain explicit. This preserves the required pins of the
            // former receiver projections independently of whether source member sugar exists.
            if (e.Mode != "command" && parameters.FirstOrDefault(p => !p.Omit) is { Type: "Object" } subject)
                subject.Default = "null";
            string receiverType = e.Mode == "command" ? "Object" : parameters.FirstOrDefault(p => !p.Omit)?.Type ?? "";
            if (e.Receiver.Length > 0)
            {
                if (!receivers.Add(receiverType + "." + e.Receiver)) Error(5, "Duplicate receiver " + receiverType + "." + e.Receiver);
                if (receiverType is not ("Location" or "Effect")) { Error(9, "LanguageValue receiver must be a typed Location or Effect, never an Object/action subject: " + e.Name); invalid = true; }
                if (parameters.Count > 0 && e.Mode != "command") parameters.First(p => !p.Omit).Default = "null";
            }
            if (invalid) continue;
            string className = "Nwn" + method.Name + "Executor";
            registrations.Append($"        glyph.Add<{Nwn}.{className}>();\n");
            wrappers.Append($"internal sealed class {className} : GlyphNodeBase\n{{\n    public override string TypeId => Descriptor.TypeId;\n    public override GlyphNodeDefinition CreateDefinition() => Descriptor.CreateDefinition();\n    public static GlyphIntrinsicDescriptor Descriptor {{ get; }} = new()\n    {{\n        TypeId = {Literal(e.Name)}, DisplayName = {Literal(method.Name)}, Category = {Literal("NWN / " + e.Group)},\n        Description = {Literal(e.Description.Length > 0 ? e.Description : Documentation(method))}, Source = {Literal("NWScript." + method.Name)}, Backend = {Literal(e.Mode == "command" ? "NWScript.AssignCommand" : "NWScript")}, Deprecated = {(e.Deprecated.Length > 0 ? Literal(e.Deprecated) : "null")},\n        Archetype = GlyphNodeArchetype.{(e.Mode == "pure" ? "PureFunction" : "Action")},\n        Parameters = [\n");
            if (e.Mode == "command") wrappers.Append("            Pins.InObject(\"actor\", \"Actor\"),\n");
            foreach (var p in parameters.Where(p => !p.Omit)) wrappers.Append($"            Pins.In({Literal(p.Pin)}, {Literal(p.Pin)}, GlyphDataType.{RuntimeType(p.Type)}, {p.Default}),\n");
            wrappers.Append("        ],\n        Results = [");
            if (resultType != "Void") wrappers.Append($"Pins.Out(\"value\", \"Value\", GlyphDataType.{RuntimeType(resultType)})");
            wrappers.Append("],\n        Exports = [\n");
            wrappers.Append($"            new({Literal(e.Name)}, {(resultType == "Void" ? "null" : "\"value\"")}" + (e.Receiver.Length > 0 ? $", ReceiverMethods: [{Literal(e.Receiver)}], ReceiverType: GlyphDataType.{RuntimeType(receiverType)}, ReceiverPolicy: GlyphReceiverPolicy.LanguageValue" : "") + "),\n");
            foreach (string alias in e.Aliases.Split(',').Where(s => s.Length > 0)) wrappers.Append($"            new({Literal(alias)}, {(resultType == "Void" ? "null" : "\"value\"")}),\n");
            wrappers.Append("        ]\n    };\n    public override async Task<GlyphNodeResult> RunAsync(GlyphNodeContext cx)\n    {\n");
            if (e.Mode == "command") wrappers.Append("        uint actor = await cx.InObject(\"actor\");\n");
            int idx = 0;
            foreach (var p in parameters) { if (!p.Omit) wrappers.Append(Read(p, "p" + idx)); idx++; }
            var requiredStructures = parameters.Select((p, i) => (p, i)).Where(x => !x.p.Omit && x.p.Type is "Location" or "Effect" && !x.p.Symbol.IsOptional).Select(x => "p" + x.i + " == IntPtr.Zero").ToArray();
            if (requiredStructures.Length > 0)
            {
                string fallback = resultType switch { "Object" => "NWScript.OBJECT_INVALID", "String" => "string.Empty", "Bool" => "false", "Int" => "0", "Float" => "0d", "Location" => "default(GlyphNwnLocation)", "Effect" => "default(GlyphNwnEffect)", _ => "" };
                wrappers.Append($"        if ({string.Join(" || ", requiredStructures)}) return new GlyphNodeResult {{ NextExecPinId = {(e.Mode == "pure" ? "null" : "\"exec_out\"")}, OutputValues = new() {{" + (resultType == "Void" ? "" : $" [\"value\"] = {fallback} ") + "} };\n");
            }
            string args = string.Join(", ", parameters.Select((p, i) => p.Omit ? NativeDefault(p.Symbol) : "p" + i));
            string invocation = $"NWScript.{method.Name}({args})";
            if (e.Mode == "command") wrappers.Append($"        if (actor != NWScript.OBJECT_INVALID) NWScript.AssignCommand(actor, () => {invocation});\n");
            else wrappers.Append("        " + (resultType == "Void" ? "" : "var value = ") + invocation + ";\n");
            wrappers.Append($"        return new GlyphNodeResult {{ NextExecPinId = {(e.Mode == "pure" ? "null" : "\"exec_out\"")}, OutputValues = new() {{");
            if (resultType != "Void") wrappers.Append($" [\"value\"] = {ConvertReturn(resultType, "value", method.ReturnType)} ");
            wrappers.Append("} };\n    }\n}\n");
        }
        cx.AddSource("GlyphNwnExecutors.g.cs", wrappers.ToString());
        cx.AddSource("GlyphNwnRegistration.g.cs", $"// <auto-generated/>\nnamespace {Platform};\npublic static partial class GlyphGeneratedRegistry\n{{\n    static partial void RegisterNwn(GlyphModuleBuilder glyph)\n    {{\n{registrations}    }}\n}}\n");
        var constants = new StringBuilder();
        var constantCoverage = new StringBuilder();
        var constantNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in api.GetMembers().OfType<IFieldSymbol>().Where(f => f.IsConst && f.DeclaredAccessibility == Accessibility.Public).OrderBy(f => f.Name, StringComparer.Ordinal))
        {
            string? prefix = domains.Keys.OrderByDescending(p => p.Length).FirstOrDefault(p => field.Name.StartsWith(p + "_", StringComparison.Ordinal));
            string type = DefaultType(field.Type);
            if (prefix == null || type.Length == 0)
            {
                constantCoverage.Append($"        new({Literal(field.Name)}, {Literal(field.Type.ToDisplayString())}, {Literal(Convert.ToString(field.ConstantValue, CultureInfo.InvariantCulture) ?? "")}, null),\n");
                continue;
            }
            string group = domains[prefix], suffix = field.Name.Substring(prefix.Length + 1);
            string name = group + "." + (SyntaxFacts.IsValidIdentifier(suffix) ? suffix : "VALUE_" + suffix);
            if (!constantNames.Add(name)) { Error(6, "Constant collision " + name); continue; }
            string value = field.Type.SpecialType == SpecialType.System_Boolean ? ((bool)field.ConstantValue! ? "true" : "false") :
                field.Type.SpecialType == SpecialType.System_String ? Literal((string)field.ConstantValue!) :
                field.Type.SpecialType == SpecialType.System_UInt32 ? Convert.ToString(field.ConstantValue, CultureInfo.InvariantCulture) + "u" :
                Convert.ToString(field.ConstantValue, CultureInfo.InvariantCulture) + (type == "Float" ? "d" : "");
            constants.Append($"        new({Literal(name)}, {Literal(group)}, {Literal(type)}, {value}, {Literal("NWScript." + field.Name)}, {Literal(field.Name)}),\n");
            constantCoverage.Append($"        new({Literal(field.Name)}, {Literal(field.Type.ToDisplayString())}, {Literal(Convert.ToString(field.ConstantValue, CultureInfo.InvariantCulture) ?? "")}, {Literal(name)}),\n");
        }
        cx.AddSource("GlyphNwnSurface.g.cs", $"// <auto-generated/>\n#nullable enable\nnamespace {Nwn};\npublic static class GlyphNwnSurface\n{{\n    public const string ApiVersion = {Literal(api.ContainingAssembly.Identity.ToString())};\n    public static System.Collections.Generic.IReadOnlyList<GlyphNwnBindingCoverage> Bindings {{ get; }} = System.Array.AsReadOnly(new GlyphNwnBindingCoverage[] {{\n{coverage}    }});\n    public static System.Collections.Generic.IReadOnlyList<GlyphNwnConstant> Constants {{ get; }} = System.Array.AsReadOnly(new GlyphNwnConstant[] {{\n{constants}    }});\n    public static System.Collections.Generic.IReadOnlyList<GlyphNwnConstantCoverage> ConstantCoverage {{ get; }} = System.Array.AsReadOnly(new GlyphNwnConstantCoverage[] {{\n{constantCoverage}    }});\n}}\n");
    }
}
