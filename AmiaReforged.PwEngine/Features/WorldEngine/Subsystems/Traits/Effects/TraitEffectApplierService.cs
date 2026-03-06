using System.Text.Json;
using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;

/// <summary>
///     Applies trait effects (skill/ability modifiers) to player creatures as NWN permanent effects.
///     Subscribes to module enter, level up, and respawn events to reapply effects automatically.
///     Also exposes <see cref="ApplyTraits" /> for on-demand application after trait confirmation.
/// </summary>
[ServiceBinding(typeof(TraitEffectApplierService))]
public class TraitEffectApplierService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly TraitEffectApplicationService _effectService;
    private readonly ICharacterTraitRepository _characterTraitRepo;
    private readonly ITraitRepository _traitRepo;
    private readonly AbilityResolver _abilityResolver = new();
    private readonly SkillResolver _skillResolver = new();

    public TraitEffectApplierService(
        TraitEffectApplicationService effectService,
        ICharacterTraitRepository characterTraitRepo,
        ITraitRepository traitRepo)
    {
        _effectService = effectService;
        _characterTraitRepo = characterTraitRepo;
        _traitRepo = traitRepo;

        NwModule.Instance.OnClientEnter += OnClientEnter;
        NwModule.Instance.OnPlayerLevelUp += OnPlayerLevelUp;
        NwModule.Instance.OnPlayerRespawn += OnPlayerRespawn;

        Log.Info("TraitEffectApplierService initialized.");
    }

    /// <summary>
    ///     Strips existing trait effects and reapplies all active, confirmed trait effects
    ///     to the player's creature.
    /// </summary>
    /// <param name="player">The player whose creature should receive trait effects.</param>
    public void ApplyTraits(NwPlayer player)
    {
        NwCreature? creature = player.LoginCreature;
        if (creature == null) return;

        Guid characterId = PcKeyUtils.GetPcKey(player);
        if (characterId == Guid.Empty) return;

        RefundOrphanedTraits(player, characterId);
        StripTraitEffects(creature);
        ApplyActiveTraitEffects(creature, characterId);
    }

    private void OnClientEnter(ModuleEvents.OnClientEnter e) => ApplyTraits(e.Player);

    private void OnPlayerLevelUp(ModuleEvents.OnPlayerLevelUp e) => ApplyTraits(e.Player);

    private void OnPlayerRespawn(ModuleEvents.OnPlayerRespawn e) => ApplyTraits(e.Player);

    /// <summary>
    ///     Detects character traits whose definitions no longer exist in the in-memory repository,
    ///     deletes them from the database, and records the refund on the player's <c>ds_pckey</c> item.
    ///     Budget points are freed automatically because <see cref="TraitBudget" /> derives used points
    ///     from the character_traits table — removing the row restores the points.
    /// </summary>
    /// <param name="player">The player whose <c>ds_pckey</c> will store the refund audit trail.</param>
    /// <param name="characterId">The persisted character identifier.</param>
    private void RefundOrphanedTraits(NwPlayer player, Guid characterId)
    {
        List<CharacterTrait> allTraits =
            _characterTraitRepo.GetByCharacterId(CharacterId.From(characterId));

        List<CharacterTrait> orphans = allTraits
            .Where(ct => _traitRepo.Get(ct.TraitTag) == null)
            .ToList();

        if (orphans.Count == 0) return;

        foreach (CharacterTrait orphan in orphans)
        {
            _characterTraitRepo.Delete(orphan.Id);
            Log.Info($"Deleted orphaned trait '{orphan.TraitTag}' for character {characterId}.");
        }

        NwItem? pcKey = player.LoginCreature?.Inventory.Items
            .FirstOrDefault(i => i.ResRef == "ds_pckey");

        if (pcKey != null)
        {
            OrphanedTraitRecorder.Record(pcKey, orphans);
        }

        player.SendServerMessage(
            $"{orphans.Count} trait(s) were removed because their definitions no longer exist. Points refunded.",
            ColorConstants.Orange);
    }

    /// <summary>
    ///     Removes all effects whose tag starts with <see cref="TraitEffectApplicationService.EffectTagPrefix" />.
    /// </summary>
    /// <param name="creature">The creature to strip trait effects from.</param>
    private static void StripTraitEffects(NwCreature creature)
    {
        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.Tag != null && effect.Tag.StartsWith(TraitEffectApplicationService.EffectTagPrefix))
            {
                creature.RemoveEffect(effect);
            }
        }
    }

    /// <summary>
    ///     Fetches active trait effects for the character, converts them to NWN effects
    ///     tagged with <c>TRAIT_{traitTag}</c>, and applies them permanently.
    /// </summary>
    /// <param name="creature">The creature to apply effects to.</param>
    /// <param name="characterId">The persisted character identifier.</param>
    private void ApplyActiveTraitEffects(NwCreature creature, Guid characterId)
    {
        List<(string TraitTag, TraitEffect Effect)> activeEffects = _effectService.GetActiveEffects(characterId);
        if (activeEffects.Count == 0) return;

        foreach ((string traitTag, TraitEffect traitEffect) in activeEffects)
        {
            Effect? nwnEffect = MapToNwnEffect(traitEffect);
            if (nwnEffect == null) continue;

            nwnEffect.Tag = TraitEffectApplicationService.EffectTagPrefix + traitTag;
            nwnEffect.SubType = EffectSubType.Supernatural;

            creature.ApplyEffect(EffectDuration.Permanent, nwnEffect);
        }

        Log.Info($"Applied {activeEffects.Count} trait effect(s) to {creature.Name}.");
    }

    /// <summary>
    ///     Converts a domain <see cref="TraitEffect" /> into the corresponding NWN <see cref="Effect" />.
    /// </summary>
    /// <param name="traitEffect">The domain trait effect to convert.</param>
    /// <returns>The NWN effect, or <c>null</c> if the effect type has no NWN representation.</returns>
    private Effect? MapToNwnEffect(TraitEffect traitEffect)
    {
        return traitEffect.EffectType switch
        {
            TraitEffectType.AttributeModifier => _abilityResolver.Resolve(traitEffect),
            TraitEffectType.SkillModifier => _skillResolver.Resolve(traitEffect),
            _ => null
        };
    }

    /// <summary>
    ///     Appends orphaned-trait refund records to the <c>trait_refunds_json</c> local variable
    ///     on the player's <c>ds_pckey</c> item. The variable persists across server resets
    ///     because it is stored on a BIC-saved inventory item.
    /// </summary>
    private static class OrphanedTraitRecorder
    {
        private const string VarName = "trait_refunds_json";

        /// <summary>
        ///     Appends one <see cref="TraitRefundRecord" /> per orphan to the existing JSON array
        ///     on the <paramref name="pcKey" /> item.
        /// </summary>
        /// <param name="pcKey">The <c>ds_pckey</c> inventory item.</param>
        /// <param name="orphans">The orphaned character traits being refunded.</param>
        public static void Record(NwItem pcKey, List<CharacterTrait> orphans)
        {
            LocalVariableString local = pcKey.GetObjectVariable<LocalVariableString>(VarName);

            List<TraitRefundRecord> records = DeserializeExisting(local);

            foreach (CharacterTrait orphan in orphans)
            {
                records.Add(new TraitRefundRecord(orphan.TraitTag, DateTime.UtcNow));
            }

            local.Value = JsonSerializer.Serialize(records);
        }

        private static List<TraitRefundRecord> DeserializeExisting(LocalVariableString local)
        {
            if (!local.HasValue || string.IsNullOrWhiteSpace(local.Value))
                return new List<TraitRefundRecord>();

            try
            {
                return JsonSerializer.Deserialize<List<TraitRefundRecord>>(local.Value)
                       ?? new List<TraitRefundRecord>();
            }
            catch (JsonException)
            {
                return new List<TraitRefundRecord>();
            }
        }
    }

    /// <summary>
    ///     Immutable record stored in the <c>trait_refunds_json</c> local variable
    ///     on <c>ds_pckey</c> to provide an audit trail of orphaned-trait refunds.
    /// </summary>
    /// <param name="Tag">The tag of the trait definition that was removed.</param>
    /// <param name="RefundedAt">UTC timestamp when the refund occurred.</param>
    private record TraitRefundRecord(string Tag, DateTime RefundedAt);

    /// <summary>
    ///     Resolves <see cref="TraitEffect" /> instances targeting an ability (Strength, Intelligence, etc.)
    ///     into the corresponding NWN <see cref="Effect.AbilityIncrease" /> or <see cref="Effect.AbilityDecrease" />.
    /// </summary>
    private sealed class AbilityResolver
    {
        private static readonly Dictionary<string, Ability> Map = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Strength"] = Ability.Strength,
            ["Dexterity"] = Ability.Dexterity,
            ["Constitution"] = Ability.Constitution,
            ["Intelligence"] = Ability.Intelligence,
            ["Wisdom"] = Ability.Wisdom,
            ["Charisma"] = Ability.Charisma
        };

        /// <summary>
        ///     Converts an ability-targeting <see cref="TraitEffect" /> to an NWN effect.
        /// </summary>
        /// <param name="traitEffect">The trait effect whose <see cref="TraitEffect.Target" /> names an ability.</param>
        /// <returns>The NWN effect, or <c>null</c> if the target is unrecognized.</returns>
        public Effect? Resolve(TraitEffect traitEffect)
        {
            if (string.IsNullOrWhiteSpace(traitEffect.Target)) return null;

            if (!Map.TryGetValue(traitEffect.Target, out Ability ability))
            {
                Log.Warn($"Unknown ability target '{traitEffect.Target}' in trait effect.");
                return null;
            }

            return traitEffect.Magnitude >= 0
                ? Effect.AbilityIncrease(ability, traitEffect.Magnitude)
                : Effect.AbilityDecrease(ability, Math.Abs(traitEffect.Magnitude));
        }
    }

    /// <summary>
    ///     Resolves <see cref="TraitEffect" /> instances targeting a skill (Hide, Spot, etc.)
    ///     into the corresponding NWN <see cref="Effect.SkillIncrease" /> or <see cref="Effect.SkillDecrease" />.
    ///     The skill lookup table is built lazily because <see cref="NwSkill" /> statics
    ///     are unavailable until the module has loaded.
    /// </summary>
    private sealed class SkillResolver
    {
        private Dictionary<string, NwSkill>? _map;

        /// <summary>
        ///     Converts a skill-targeting <see cref="TraitEffect" /> to an NWN effect.
        /// </summary>
        /// <param name="traitEffect">The trait effect whose <see cref="TraitEffect.Target" /> names a skill.</param>
        /// <returns>The NWN effect, or <c>null</c> if the target is unrecognized.</returns>
        public Effect? Resolve(TraitEffect traitEffect)
        {
            if (string.IsNullOrWhiteSpace(traitEffect.Target)) return null;

            Dictionary<string, NwSkill> map = EnsureMap();

            if (!map.TryGetValue(traitEffect.Target, out NwSkill? skill))
            {
                Log.Warn($"Unknown skill target '{traitEffect.Target}' in trait effect.");
                return null;
            }

            return traitEffect.Magnitude >= 0
                ? Effect.SkillIncrease(skill, traitEffect.Magnitude)
                : Effect.SkillDecrease(skill, Math.Abs(traitEffect.Magnitude));
        }

        private Dictionary<string, NwSkill> EnsureMap()
        {
            if (_map != null) return _map;

            _map = new Dictionary<string, NwSkill>(StringComparer.OrdinalIgnoreCase);

            TryAdd("AnimalEmpathy", Skill.AnimalEmpathy);
            TryAdd("Appraise", Skill.Appraise);
            TryAdd("Bluff", Skill.Bluff);
            TryAdd("Concentration", Skill.Concentration);
            TryAdd("CraftArmor", Skill.CraftArmor);
            TryAdd("CraftTrap", Skill.CraftTrap);
            TryAdd("CraftWeapon", Skill.CraftWeapon);
            TryAdd("DisableTrap", Skill.DisableTrap);
            TryAdd("Discipline", Skill.Discipline);
            TryAdd("Heal", Skill.Heal);
            TryAdd("Hide", Skill.Hide);
            TryAdd("Intimidate", Skill.Intimidate);
            TryAdd("Listen", Skill.Listen);
            TryAdd("Lore", Skill.Lore);
            TryAdd("MoveSilently", Skill.MoveSilently);
            TryAdd("OpenLock", Skill.OpenLock);
            TryAdd("Parry", Skill.Parry);
            TryAdd("Perform", Skill.Perform);
            TryAdd("Persuade", Skill.Persuade);
            TryAdd("PickPocket", Skill.PickPocket);
            TryAdd("Ride", Skill.Ride);
            TryAdd("Search", Skill.Search);
            TryAdd("SetTrap", Skill.SetTrap);
            TryAdd("Spellcraft", Skill.Spellcraft);
            TryAdd("Spot", Skill.Spot);
            TryAdd("Taunt", Skill.Taunt);
            TryAdd("Tumble", Skill.Tumble);
            TryAdd("UseMagicDevice", Skill.UseMagicDevice);

            return _map;
        }

        private void TryAdd(string name, NwSkill? skill)
        {
            if (skill != null) _map!.Add(name, skill);
        }
    }
}
