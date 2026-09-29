using AmiaReforged.PwEngine.Features.Glyph.Core;
namespace AmiaReforged.PwEngine.Features.Glyph.Language.Binding;

public enum GlyphLoweringStrategy { Value, Action, PredicateBranch }

/// <summary>
/// Curated receiver-method sugar: <c>receiver.method(args)</c> where <c>receiver</c> is an
/// Object-typed Glyph expression. It lowers to the static catalog intrinsic <c>Target</c> with
/// the bound receiver injected as parameter zero — no new executor is introduced. The receiver
/// type is the Glyph value type of the expression (here <see cref="GlyphDataType.NwObject"/>),
/// not the capitalized <c>Object.</c> namespace spelling.
/// </summary>
public sealed record GlyphReceiverMethod(GlyphDataType ReceiverType, string Name, string Target);

public sealed record GlyphLanguageSymbol(string Name, GlyphNodeDefinition Definition, string? OutputPin,
    GlyphLoweringStrategy Strategy, string[]? AllowedStages = null)
{
    public IReadOnlyList<GlyphPin> Parameters => Definition.InputPins.Where(p => p.DataType != GlyphDataType.Exec).ToArray();
    public GlyphTypeSymbol ReturnType => Strategy == GlyphLoweringStrategy.PredicateBranch ? GlyphTypeSymbol.Bool :
        OutputPin == null ? GlyphTypeSymbol.Void : GlyphTypeSymbol.From(Definition.OutputPins.Single(p => p.Id == OutputPin).DataType);
}

/// <summary>Language spelling and lowering policy; runtime metadata owns all pin signatures.</summary>
public sealed class GlyphLanguageCatalog
{
    private readonly Dictionary<string, GlyphLanguageSymbol> _symbols = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GlyphReceiverMethod> _receiverMethods = new(StringComparer.Ordinal);
    public IReadOnlyCollection<GlyphLanguageSymbol> Symbols => _symbols.Values;
    public IReadOnlyCollection<GlyphReceiverMethod> ReceiverMethods => _receiverMethods.Values;
    public GlyphLanguageSymbol? Find(string name) => _symbols.GetValueOrDefault(name);
    /// <summary>
    /// Resolves a registered receiver method by its terminal member name. The caller binds the
    /// receiver expression first, then verifies its Glyph type matches <c>receiverType</c>.
    /// </summary>
    public bool TryResolveReceiverMethod(string name, out GlyphReceiverMethod method)
        => _receiverMethods.TryGetValue(name, out method!);
    public IGlyphNodeDefinitionRegistry Registry { get; }
    public GlyphLanguageCatalog(IGlyphNodeDefinitionRegistry registry)
    {
        Registry = registry;
        Add("heal", "action.heal_creature");
        Add("damage", "action.damage_creature");
        Add("message", "action.send_message");
        Add("floating_text", "action.send_floating_text");
        Add("play_vfx", "action.play_vfx");
        Add("set_name", "action.set_creature_name");
        Add("spawn.modify_count", "action.modify_spawn_count");
        Add("spawn.cancel", "action.cancel_spawn");
        Add("spawn.skip_bonuses", "action.skip_bonuses");
        Add("spawn.skip_mutations", "action.skip_mutations");
        Add("spawn_resource_node", "action.spawn_resource_node");
        Add("distance", "getter.distance_between", "distance");
        Add("random", "getter.random_int", "result");
        Add("party.members", "getter.party_members", "members");
        Add("creature.hp", "getter.creature_hp", "current_hp");
        Add("creature.max_hp", "getter.creature_hp", "max_hp");
        Add("creature.name", "getter.creature_name", "name");
        Add("creature.ac", "getter.creature_ac", "ac");
        Add("has_item", "getter.has_item", "has_item");
        // Curated NWN object surface (see Language/README.md). Dotted names resolve
        // through the binder's path lookup; each is backed by a registered getter.
        Add(
            "Object.get_distance",
            "getter.distance_between",
            "distance"
        );
        Add("Object.nearest_object_by_type", "getter.nearest_object_by_type", "object");
        Add("Object.is_player", "getter.is_player", "result");
        // Curated Object receiver sugar. Each lowers to the static intrinsic above with the
        // bound receiver injected as parameter zero (object_a). Add future curated Object members here.
        AddReceiverMethod(GlyphDataType.NwObject, "get_nearest_object_by_type", "Object.nearest_object_by_type");
        AddReceiverMethod(GlyphDataType.NwObject, "is_player", "Object.is_player");
        AddReceiverMethod(GlyphDataType.NwObject, "get_distance", "Object.get_distance");
        Add("has_knowledge", "knowledge.has", "result");
        Add("industry.is_member", "industry.is_member", "result");
        Add("industry.level", "industry.get_level", "level_value");
        Add("has_trait", "trait.has_trait", "has_trait");
        Add("fail", "interaction.fail");
        Add("set_progress", "interaction.set_progress", stages: ["started", "tick"]);
        Add("set_required_rounds", "interaction.set_required_rounds", stages: ["started", "tick"]);
        Add("set_status", "interaction.set_status", stages: ["started", "tick", "completed"]);
        Add("set_metadata", "interaction.set_metadata");
        Add("metadata", "interaction.get_metadata", "value");
        Add("store_session_object", "interaction.store_session_object", stages: ["started", "tick", "completed"]);
        Add("session_object", "interaction.retrieve_session_object", "object", stages: ["started", "tick", "completed"]);
        Add("skill_check", "interaction.skill_check", strategy: GlyphLoweringStrategy.PredicateBranch);
    }
    private void AddReceiverMethod(GlyphDataType receiverType, string name, string target)
        => _receiverMethods.Add(name, new GlyphReceiverMethod(receiverType, name, target));

    private void Add(string name, string type, string? output = null, string[]? stages = null, GlyphLoweringStrategy? strategy = null)
    {
        GlyphNodeDefinition def = Registry.Get(type) ?? throw new InvalidOperationException($"Missing runtime intrinsic {type}.");
        if (output != null && !def.OutputPins.Any(p => p.Id == output))
            throw new InvalidOperationException($"Missing output {type}.{output}.");
        _symbols.Add(name, new(name, def, output, strategy ?? (output == null ? GlyphLoweringStrategy.Action : GlyphLoweringStrategy.Value), stages));
    }
    public static readonly IReadOnlyDictionary<string, GlyphEventType> Events = new Dictionary<string, GlyphEventType>
    {
        ["encounter.before_group_spawn"] = GlyphEventType.BeforeGroupSpawn,
        ["encounter.after_group_spawn"] = GlyphEventType.AfterGroupSpawn,
        ["encounter.on_creature_spawn"] = GlyphEventType.OnCreatureSpawn,
        ["encounter.on_creature_death"] = GlyphEventType.OnCreatureDeath,
        ["encounter.on_boss_spawn"] = GlyphEventType.OnBossSpawn,
        ["trait.on_granted"] = GlyphEventType.OnTraitGranted,
        ["trait.on_removed"] = GlyphEventType.OnTraitRemoved,
        ["interaction"] = GlyphEventType.InteractionPipeline
    };
    public static string EntryType(GlyphEventType evt) => evt switch
    {
        GlyphEventType.BeforeGroupSpawn => "event.before_group_spawn",
        GlyphEventType.AfterGroupSpawn => "event.after_group_spawn",
        GlyphEventType.OnCreatureSpawn => "event.on_creature_spawn",
        GlyphEventType.OnCreatureDeath => "event.on_creature_death",
        GlyphEventType.OnBossSpawn => "event.on_boss_spawn",
        GlyphEventType.OnTraitGranted => "event.on_trait_granted",
        GlyphEventType.OnTraitRemoved => "event.on_trait_removed",
        _ => throw new ArgumentOutOfRangeException(nameof(evt))
    };
}
