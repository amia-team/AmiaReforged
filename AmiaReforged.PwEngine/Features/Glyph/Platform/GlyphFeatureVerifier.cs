using System.Globalization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Language.Binding;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;

namespace AmiaReforged.PwEngine.Features.Glyph.Platform;

/// <summary>Checks the assembled contract, including custom definitions and module factories.</summary>
public static class GlyphFeatureVerifier
{
    public static void Verify(IGlyphNodeDefinitionRegistry registry, IReadOnlyList<IGlyphNodeExecutor>? executors = null)
    {
        var errors = Errors(registry, executors);
        if (errors.Count > 0) throw new InvalidOperationException("Invalid Glyph platform:\n" + string.Join("\n", errors));
    }

    public static IReadOnlyList<string> Errors(IGlyphNodeDefinitionRegistry registry,
        IReadOnlyList<IGlyphNodeExecutor>? executors = null)
    {
        List<string> errors = [];
        var definitions = registry.GetAll();
        var exports = definitions.SelectMany(d => d.Intrinsics.Select(i => (Definition: d, Intrinsic: i))).ToArray();
        var names = exports.Select(e => e.Intrinsic.Name).ToHashSet(StringComparer.Ordinal);
        void Check(bool condition, string message) { if (!condition) errors.Add(message); }
        void Unique(IEnumerable<string> values, string scope)
        {
            foreach (var group in values.GroupBy(v => v, StringComparer.Ordinal))
                Check(!string.IsNullOrWhiteSpace(group.Key) && group.Count() == 1, $"{scope}: duplicate/empty ID '{group.Key}'.");
        }
        Unique(definitions.Select(d => d.TypeId), "runtime");
        Unique(exports.Select(e => e.Intrinsic.Name), "source");
        Unique(GlyphEvents.All.Select(e => e.Name), "events");
        Unique(GlyphEvents.All.Select(e => e.EventType.ToString()), "event identities");
        foreach (var identity in Enum.GetValues<GlyphEventType>())
            Check(GlyphEvents.All.Any(e => e.EventType == identity), $"Missing event declaration '{identity}'.");
        if (executors != null)
        {
            Unique(executors.Select(e => e.TypeId), "executors");
            foreach (var executor in executors)
                Check(registry.Get(executor.TypeId)?.TypeId == executor.CreateDefinition().TypeId,
                    $"Executor '{executor.TypeId}' disagrees with its definition.");
            foreach (var def in definitions)
                Check(executors.Any(e => e.TypeId == def.TypeId), $"Missing executor '{def.TypeId}'.");
        }
        foreach (var def in definitions)
        {
            Unique(def.InputPins.Select(p => p.Id), def.TypeId + " inputs");
            Unique(def.OutputPins.Select(p => p.Id), def.TypeId + " outputs");
            Check(def.InputPins.All(p => p.Direction == GlyphPinDirection.Input) &&
                  def.OutputPins.All(p => p.Direction == GlyphPinDirection.Output), $"{def.TypeId}: invalid pin direction.");
            if (def.RestrictToEventType is { } evt)
                Check(GlyphEvents.All.Any(e => e.EventType == evt), $"{def.TypeId}: unknown event restriction '{evt}'.");
            foreach (var pin in def.InputPins.Where(p => p.DefaultValue != null))
                Check(ValidDefault(pin), $"{def.TypeId}.{pin.Id}: invalid {pin.DataType} default '{pin.DefaultValue}'.");
            if (def.ContextSchema is { } schema)
            {
                Unique(schema.Fields.Select(f => f.PinId), def.TypeId + " context");
                Unique(schema.Fields.SelectMany(f => new[] { f.PinId, "context." + f.PinId, "chaos." + f.PinId }.Concat(f.Aliases ?? [])), def.TypeId + " context aliases");
                foreach (var field in schema.Fields)
                {
                    Check(def.OutputPins.Any(p => p.Id == field.PinId && p.DataType == field.DataType),
                        $"{def.TypeId}.{field.PinId}: missing schema output.");
                    var getter = registry.Get($"context.{def.TypeId}.{field.PinId}");
                    Check(getter?.ContextSourceTypeId == def.TypeId && getter.OutputPins.Any(p => p.Id == "value" && p.DataType == field.DataType),
                        $"{def.TypeId}.{field.PinId}: missing schema getter.");
                }
            }
        }
        var aliases = exports.SelectMany(e => (e.Intrinsic.CallAliases ?? []).Concat(e.Intrinsic.PropertyAliases ?? [])).ToArray();
        Unique(exports.Select(e => e.Intrinsic.Name).Concat(exports.SelectMany(e => e.Intrinsic.CallAliases ?? []).Select(a => a.Name)), "call spellings");
        Unique(exports.SelectMany(e => e.Intrinsic.PropertyAliases ?? []).Select(a => a.Name), "property aliases");
        foreach (var alias in aliases) Check(names.Contains(alias.Target), $"Alias '{alias.Name}' has unknown target '{alias.Target}'.");
        foreach (var alias in aliases.Where(a => a.ImplicitArgument != null && names.Contains(a.Target)))
        {
            var target = exports.First(e => e.Intrinsic.Name == alias.Target);
            var parameter = target.Definition.InputPins.FirstOrDefault(p => p.DataType != GlyphDataType.Exec);
            Check(parameter != null && definitions.Where(d => d.ContextSchema != null).Any(d =>
                (target.Definition.RestrictToEventType == null || target.Definition.RestrictToEventType == d.RestrictToEventType) &&
                (target.Definition.ScriptCategory == null || target.Definition.ScriptCategory == d.ScriptCategory) &&
                d.ContextSchema!.Fields.Any(f => f.PinId == d.ContextSchema.Resolve(alias.ImplicitArgument!) && f.DataType == parameter.DataType)),
                $"Alias '{alias.Name}' has no compatible implicit context argument '{alias.ImplicitArgument}'.");
        }
        var indexers = exports.Where(e => e.Intrinsic.Indexer != null).Select(e => e.Intrinsic.Indexer!).ToArray();
        Unique(indexers.Select(i => i.Name), "indexers");
        foreach (var indexer in indexers)
        {
            var getter = exports.FirstOrDefault(e => e.Intrinsic.Name == indexer.Getter);
            var setter = exports.FirstOrDefault(e => e.Intrinsic.Name == indexer.Setter);
            if (getter.Definition == null || setter.Definition == null)
            {
                errors.Add($"Indexer '{indexer.Name}' has an unknown getter/setter.");
                continue;
            }
            var inputs = getter.Definition.InputPins.Where(p => p.DataType != GlyphDataType.Exec).ToArray();
            var setterInputs = setter.Definition.InputPins.Where(p => p.DataType != GlyphDataType.Exec).ToArray();
            var output = getter.Definition.OutputPins.FirstOrDefault(p => p.Id == getter.Intrinsic.OutputPin);
            Check(inputs.Length == 1 && setterInputs.Length == 2 && output != null &&
                inputs[0].DataType == setterInputs[0].DataType && output.DataType == setterInputs[1].DataType &&
                setter.Definition.Archetype == GlyphNodeArchetype.Action, $"Indexer '{indexer.Name}' has incompatible signatures.");
        }
        Unique(exports.SelectMany(e => (e.Intrinsic.ReceiverMethods ?? []).Select(r => e.Intrinsic.ReceiverType + "." + r)), "receiver methods");
        Unique(exports.Where(e => e.Intrinsic.WritableAs != null).Select(e => e.Intrinsic.WritableAs!), "writable aliases");
        foreach (var (def, intrinsic) in exports)
        {
            string prefix = $"{intrinsic.Name} ({def.TypeId})";
            var strategy = intrinsic.Strategy ?? (def.Archetype == GlyphNodeArchetype.Action || intrinsic.OutputPin == null ? GlyphLoweringStrategy.Action : GlyphLoweringStrategy.Value);
            Check(intrinsic.OutputPin == null || def.OutputPins.Any(p => p.Id == intrinsic.OutputPin && p.DataType != GlyphDataType.Exec),
                prefix + ": missing return output.");
            Check(strategy != GlyphLoweringStrategy.Value || intrinsic.OutputPin != null, prefix + ": value function needs a return output.");
            Check(strategy != GlyphLoweringStrategy.Action || def.Archetype == GlyphNodeArchetype.Action,
                prefix + ": action must use an action runtime node.");
            Check(strategy != GlyphLoweringStrategy.Value || def.Archetype == GlyphNodeArchetype.PureFunction,
                prefix + ": value function must be pure.");
            if (strategy == GlyphLoweringStrategy.PredicateBranch)
                Check(def.Archetype == GlyphNodeArchetype.FlowControl && new[] { "success", "failure" }.All(id =>
                    def.OutputPins.Any(p => p.Id == id && p.DataType == GlyphDataType.Exec)), prefix + ": invalid predicate branch outputs.");
            foreach (string stage in intrinsic.AllowedStages ?? [])
                Check(GlyphEvents.All.Any(e => e.Stages?.Any(s => s.Name == stage) == true &&
                    (def.RestrictToEventType == null || def.RestrictToEventType == e.EventType) &&
                    (def.ScriptCategory == null || def.ScriptCategory == e.Category)), prefix + $": unknown/unavailable stage '{stage}'.");
            if (intrinsic.ReceiverMethods?.Count > 0)
                Check(def.InputPins.FirstOrDefault(p => p.DataType != GlyphDataType.Exec)?.DataType == intrinsic.ReceiverType,
                    prefix + ": receiver type must match parameter zero.");
            if (intrinsic.WritableAs is { } writable)
            {
                var parameters = def.InputPins.Where(p => p.DataType != GlyphDataType.Exec).ToArray();
                Check(strategy == GlyphLoweringStrategy.Action && parameters.Length == 1,
                    prefix + ": writable alias must target a one-parameter action.");
                var fields = definitions.Where(d => d.ContextSchema != null).SelectMany(d => d.ContextSchema!.Fields).Where(f => f.PinId == writable).ToArray();
                Check(parameters.Length == 1 && fields.All(f => f.DataType == parameters[0].DataType),
                    prefix + ": writable alias type disagrees with context.");
            }
        }
        foreach (var evt in GlyphEvents.All)
        {
            var entries = evt.Stages?.Select(s => s.EntryTypeId) ?? [evt.Entry()];
            if (evt.Stages != null) Unique(evt.Stages.Select(s => s.Name), evt.Name + " stages");
            foreach (string entry in entries)
            {
                var def = registry.Get(entry);
                Check(def != null && def.RestrictToEventType == evt.EventType &&
                    def.Archetype == (evt.Stages == null ? GlyphNodeArchetype.EventEntry : GlyphNodeArchetype.PipelineStage),
                    $"Event '{evt.Name}' has invalid entry '{entry}'.");
            }
        }
        Unique(Nwn.GlyphNwnSurface.Constants.Select(c => c.Name), "standard constants");
        foreach (var constant in Nwn.GlyphNwnSurface.Constants)
        {
            var value = Nwn.GlyphStandardLibrary.Environment.GetResolvedConstant(constant.Name);
            Check(value != null && Equals(value.Value, constant.Value), $"Unresolved constant '{constant.Name}'.");
            Check(constant.Type is "Bool" or "Int" or "Float" or "String" or "Object", $"Unsupported constant type '{constant.Type}'.");
        }
        foreach (var binding in Nwn.GlyphNwnSurface.Bindings.Where(b => b.Status is "bound" or "adapted"))
            Check(binding.Name != null && names.Contains(binding.Name), $"Unregistered NWN binding '{binding.Member}' ({binding.Name}).");
        return errors.AsReadOnly();
    }

    private static bool ValidDefault(GlyphPin pin) => pin.DataType switch
    {
        GlyphDataType.String => true,
        GlyphDataType.Int => int.TryParse(pin.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        GlyphDataType.Float => double.TryParse(pin.DefaultValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number),
        GlyphDataType.Bool => bool.TryParse(pin.DefaultValue, out _),
        GlyphDataType.NwObject => uint.TryParse(pin.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
        GlyphDataType.Location or GlyphDataType.Effect => pin.DefaultValue == "invalid",
        _ => false
    };
}
