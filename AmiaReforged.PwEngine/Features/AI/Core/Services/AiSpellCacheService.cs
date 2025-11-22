using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using AmiaReforged.PwEngine.Features.AI.Core.Extensions;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Builds and caches spell lists for creatures, organized by category and caster level.
/// Ports logic from ds_ai_include.nss lines 857-1271.
/// </summary>
[ServiceBinding(typeof(AiSpellCacheService))]
public class AiSpellCacheService
{
    private readonly Dictionary<uint, CreatureSpellCache> _spellCaches = new();
    private readonly bool _isEnabled;

    public AiSpellCacheService()
    {
        _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";
    }

    /// <summary>
    /// Gets or creates spell cache for a creature.
    /// </summary>
    public CreatureSpellCache GetOrCreateCache(NwCreature creature)
    {
        if (!_isEnabled) return CreatureSpellCache.Empty;

        if (!_spellCaches.TryGetValue(creature, out var cache))
        {
            cache = BuildSpellCache(creature);
            _spellCaches[creature] = cache;
        }
        return cache;
    }

    /// <summary>
    /// Invalidates the spell cache for a creature (called on death).
    /// </summary>
    public void InvalidateCache(NwCreature creature)
    {
        if (!_isEnabled) return;
        _spellCaches.Remove(creature);
    }

    /// <summary>
    /// Builds spell cache by scanning all known spells.
    /// Port of MakeSpellList() from ds_ai_include.nss lines 857-942.
    /// </summary>
    private CreatureSpellCache BuildSpellCache(NwCreature creature)
    {
        var spellsByCasterLevel = new Dictionary<int, List<Spell>>();
        var attackSpells = new List<Spell>();
        var buffSpells = new List<Spell>();
        var healingSpells = new List<Spell>();
        var summonSpells = new List<Spell>();
        var dispelSpells = new List<Spell>();
        var persistentAoeSpells = new List<Spell>();

        int maxCasterLevel = 0;

        // Scan all possible spells (0-802 in spells.2da)
        for (int i = 0; i < 803; i++)
        {
            var spell = (Spell)i;

            if (!creature.HasSpellUse(NwSpell.FromSpellId(i))) continue;

            int baseCasterLevel = spell.GetBaseCasterLevel();
            if (baseCasterLevel == 0) continue; // Skip spells with no innate level

            int correctedCl = GetCorrectedCasterLevel(spell, baseCasterLevel);

            // Track max caster level
            if (correctedCl > maxCasterLevel)
                maxCasterLevel = correctedCl;

            // Organize by caster level
            if (!spellsByCasterLevel.ContainsKey(correctedCl))
                spellsByCasterLevel[correctedCl] = new List<Spell>();
            spellsByCasterLevel[correctedCl].Add(spell);

            // Categorize by behavior using SpellExtensions
            if (spell.IsAttackSpell())
                attackSpells.Add(spell);
            else if (spell.IsHealingSpell())
                healingSpells.Add(spell);
            else if (spell.IsBuffSpell())
                buffSpells.Add(spell);
            else if (spell.IsSummonSpell())
                summonSpells.Add(spell);
            else if (spell.IsDispelSpell())
                dispelSpells.Add(spell);
            else if (spell.IsPersistentAoeSpell())
                persistentAoeSpells.Add(spell);
        }

        return new CreatureSpellCache
        {
            MaxCasterLevel = maxCasterLevel,
            SpellsByCasterLevel = spellsByCasterLevel,
            AttackSpells = attackSpells,
            BuffSpells = buffSpells,
            HealingSpells = healingSpells,
            SummonSpells = summonSpells,
            DispelSpells = dispelSpells,
            PersistentAoeSpells = persistentAoeSpells
        };
    }

    /// <summary>
    /// Adjusts caster level for spell priority.
    /// Port of GetCorrectedCL() from ds_ai_include.nss lines 1084-1109.
    /// </summary>
    private int GetCorrectedCasterLevel(Spell spell, int baseCl)
    {
        // Boost priority of certain spells by increasing their CL
        return spell switch
        {
            // Spell.MirrorImage => 10,  // TODO: Find correct enum name
            Spell.TrueStrike => 10,
            Spell.GhostlyVisage => 8,
            // Spell.Stoneskin => 8,  // TODO: Find correct enum name
            Spell.EtherealVisage => 8,
            Spell.Haste => 7,
            Spell.ImprovedInvisibility => 7,
            Spell.Displacement => 7,
            Spell.Darkness => 6,
            // Spell.Ultravision => 5,  // TODO: Find correct enum name
            // Spell.DarkVision => 5,  // TODO: Find correct enum name
            Spell.Invisibility => 4,
            Spell.SeeInvisibility => 4,
            _ => baseCl
        };
    }
}
