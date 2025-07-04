using System.ComponentModel.DataAnnotations;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public class CraftSpell(SpellEvents.OnSpellCast eventData)
{
    private const string SpellPropertiesTableName = "iprp_spells";
    private static readonly TwoDimArray? SpellPropertiesTable = NwGameTables.GetTable(SpellPropertiesTableName);

    public void DoCraftSpell(NwItem targetItem)
    {
        if (SpellPropertiesTable == null) return;
        if (eventData.Caster == null) return;
        
        NwCreature caster = (NwCreature)eventData.Caster;
        
        if (!caster.IsPlayerControlled(out NwPlayer? player)) return;
        
        bool isEmptyScroll = targetItem.ResRef == "x2_it_cfm_bscrl";
        bool isEmptyWand = targetItem.ResRef == "x2_it_cfm_wand";
        bool isEmptyPotion = targetItem.ResRef is "x2_it_cfm_pbottl" or "it_cfm_pbten" or "x2_it_pcpotion";
        
        int? spellProp = GetHighestClSpellProp(SpellPropertiesTable);

        if (spellProp == null)
        {
            player.SendServerMessage("Craft spell failed! There is no item property associated with this spell.");
            return;
        }

        ItemProperty castSpellProperty = ItemProperty.CastSpell((IPCastSpell)spellProp, IPCastSpellNumUses.ChargePerUse1);
            
        if (isEmptyScroll)
        {
            if (!caster.KnowsFeat(Feat.ScribeScroll!)) return;

            ScribeScroll(castSpellProperty);
        }

        if (isEmptyWand)
        {
            if (!caster.KnowsFeat(Feat.CraftWand!)) return;
            if (eventData.Spell.InnateSpellLevel > 4) return;

            CraftWand(castSpellProperty);
        }

        if (isEmptyPotion)
        {
            if (!caster.KnowsFeat(Feat.BrewPotion!)) return;
            if (eventData.Spell.InnateSpellLevel > 3) return;

            BrewPotion(castSpellProperty);
        }
            
    }
    
    private void ScribeScroll(ItemProperty spellPropTable)
    {
        throw new NotImplementedException();
    }
    private void BrewPotion(ItemProperty castSpellProperty)
    {
        throw new NotImplementedException();
    }
    
    private void CraftWand(ItemProperty castSpellProperty)
    {
        throw new NotImplementedException();
    }

    private int? GetHighestClSpellProp(TwoDimArray spellPropTable)
    {
        int spellId = eventData.Spell.Id;

        List<int> spellPropRows = [];
        for (int i = 0; i < spellPropTable.RowCount; i++)
        {
            if (spellPropTable.GetInt(i, "SpellIndex") == spellId)
                spellPropRows.Add(i);
        }

        if (spellPropRows.Count == 0)
            return null;
        
        Dictionary<int, int?> spellPropClByRow = [];
        foreach (int row in spellPropRows)
        {
            int? spellPropCl = spellPropTable.GetInt(row, "CasterLvl");
            if (spellPropCl == null) continue;
            
            spellPropClByRow.Add(row, spellPropCl);
        }
        
        if (spellPropClByRow.Count == 0)
            return null;

        return spellPropClByRow.MaxBy(entry => entry.Value).Key;
    }
}