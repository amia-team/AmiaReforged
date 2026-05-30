using AmiaReforged.PwEngine.Features.AI.Core.Extensions;
using Anvil.API;
using Anvil.Services;
using AmiaReforged.PwEngine.Features.AI.Core.Models;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.AI.Core.Services;

/// <summary>
/// Builds and caches spell lists for creatures, organised by talent category and spell priority.
/// Ports logic from ds_ai_include.nss lines 857-1271.
/// </summary>
[ServiceBinding(typeof(AiSpellCacheService))]
public class AiSpellCacheService
{
    private readonly Dictionary<NwCreature, CreatureSpellCache> _spellCaches = new();
    private readonly bool _isEnabled = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE") != "live";

    /// <summary>
    /// Gets or creates a spell cache for a creature.
    /// </summary>
    public CreatureSpellCache GetOrCreateCache(NwCreature creature)
    {
        if (!_isEnabled) return CreatureSpellCache.Empty;

        if (_spellCaches.TryGetValue(creature, out CreatureSpellCache? cache)) return cache;

        cache = BuildSpellCache(creature);
        _spellCaches[creature] = cache;
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
    /// Builds a spell cache by scanning all known spells.
    /// Port of MakeSpellList() from ds_ai_include.nss lines 857-942.
    /// </summary>
    private static CreatureSpellCache BuildSpellCache(NwCreature creature)
    {
        List<CreatureSpellCache.AiSpellData> spells = [];

        foreach (NwSpell spell in NwRuleset.Spells)
        {
            if (!creature.HasSpellUse(spell)) continue;

            int spellPriority = spell.GetSpellPriority();
            SpellTalentType spellTalent = spell.GetSpellTalent();

            spells.Add(new CreatureSpellCache.AiSpellData(spell, spellPriority, spellTalent));
        }

        return new CreatureSpellCache { Spells = spells };
    }
}
