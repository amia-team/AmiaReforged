﻿using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public class CraftSpell(SpellEvents.OnSpellCast eventData, NwItem targetItem)
{
    private static readonly TwoDimArray? SpellPropTable = NwGameTables.GetTable("iprp_spells");
    private const int SpellFailVfx = 292;

    private readonly bool _isEmptyScroll = targetItem.BaseItem.ItemType == BaseItemType.BlankScroll;
    private readonly bool _isEmptyWand = targetItem.BaseItem.ItemType == BaseItemType.BlankWand;
    private readonly bool _isEmptyPotion = targetItem.BaseItem.ItemType == BaseItemType.BlankPotion;

    private const string MagicWand = "x2_it_pcwand";
    private const string MagicPotion = "x2_it_potion";

    private readonly NwSpell _spell = eventData.Spell;

    public void DoCraftSpell()
    {
        if (SpellPropTable == null) return;
        if (eventData.Caster == null) return;
        
        NwCreature caster = (NwCreature)eventData.Caster;

        if (!caster.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (!(_isEmptyScroll || _isEmptyWand || _isEmptyPotion)) return;
        
        if (caster.Inventory.Items.All(item => item != targetItem))
        {
            player.SendServerMessage($"Craft spell failed! {targetItem.Name} must be in your inventory.");
            ApplySpellCraftFailVfx(caster);
            return;
        }

        if (targetItem.HasItemProperty(ItemPropertyType.CastSpell))
        {
            player.SendServerMessage($"Craft spell failed! {targetItem.Name} has already been spell crafted.");
            ApplySpellCraftFailVfx(caster);
            return;
        }
        
        if (eventData.Item != null)
        {
            player.SendServerMessage("Craft spell failed! You can only craft spells when casting from a spellbook.");
            ApplySpellCraftFailVfx(caster);
            return;
        }
        
        (int SpellPropId, int SpellPropCl)? spellPropIdAndCl = GetSpellPropIdAndCl(SpellPropTable);

        if (spellPropIdAndCl == null)
        {
            player.SendServerMessage("Craft spell failed! There is no item property associated with this spell.");
            ApplySpellCraftFailVfx(caster);
            return;
        }
        
        int spellPropId = spellPropIdAndCl.Value.SpellPropId;
        int spellPropCl = spellPropIdAndCl.Value.SpellPropCl;
        int spellInnateLevel = _spell.InnateSpellLevel;
        
        if (_isEmptyScroll)
        {
            if (!caster.KnowsFeat(Feat.ScribeScroll!))
            {
                player.SendServerMessage("Scribe scroll failed! You don't know the feat Scribe Scroll.");
                ApplySpellCraftFailVfx(caster);
                return;
            }
            
            int scribeCost = CalculateScribeCost(spellPropCl, spellInnateLevel);
            if (caster.Gold < scribeCost)
            {
                player.SendServerMessage
                    ($"Scribe scroll failed! You don't have enough gold. The cost to scribe this stack is {scribeCost} GP.");
                ApplySpellCraftFailVfx(caster);
                return;
            }

            _ = ScribeScroll(caster, spellPropId);
            ChargeForSpellCraft(player, caster, scribeCost);
            ApplySpellCraftSuccessVfx(caster);
        }

        if (_isEmptyWand)
        {
            if (!caster.KnowsFeat(Feat.CraftWand!))
            {
                player.SendServerMessage("Craft wand failed! You don't know the feat Craft Wand.");
                ApplySpellCraftFailVfx(caster);
                return;
            }
            
            if (spellInnateLevel > 4) 
            {
                player.SendServerMessage
                    ($"Craft wand failed! Innate spell level must be 4 or lower. The innate level of this spell is {spellInnateLevel}");
                ApplySpellCraftFailVfx(caster);
                return;
            }

            int craftWandCost = CalculateCraftWandCost(spellPropCl, spellInnateLevel);
            if (caster.Gold < craftWandCost)
            {
                player.SendServerMessage
                    ($"Craft wand failed! You don't have enough gold. The cost to craft this wand is {craftWandCost} GP.");
                ApplySpellCraftFailVfx(caster);
                return;
            }
            
            NwClass? casterClass = eventData.SpellCastClass;
            if (casterClass == null)
            {
                player.SendServerMessage
                    ("Craft wand failed! Caster class wasn't recognized.");
                ApplySpellCraftFailVfx(caster);
                return;
            }
            int casterLevel = caster.Classes.First(cl => cl.Class == casterClass).Level;

            CraftWand(caster, spellPropId, craftWandCost, casterLevel);
        }

        if (_isEmptyPotion)
        {
            if (!caster.KnowsFeat(Feat.BrewPotion!))
            {
                player.SendServerMessage("Brew potion failed! You don't know the feat Brew Potion.");
                ApplySpellCraftFailVfx(caster);
                return;
            }
            
            if (spellInnateLevel > 3) 
            {
                player.SendServerMessage
                    ($"Brew potion failed! Innate spell level must be 3 or lower. The innate level of this spell is {spellInnateLevel}");
                ApplySpellCraftFailVfx(caster);
                return;
            }

            int brewPotionCost = CalculateBrewPotionCost(spellPropCl, spellInnateLevel);
            if (caster.Gold < brewPotionCost)
            {
                player.SendServerMessage
                    ($"Brew potion failed! You don't have enough gold. The cost to brew this stack is {brewPotionCost} GP.");
                ApplySpellCraftFailVfx(caster);
                return;
            }

            BrewPotion(caster, spellPropId, brewPotionCost);
        }
        
    }

    private void ChargeForSpellCraft(NwPlayer player, NwCreature caster, int spellCraftCost)
    {
        caster.Gold -= (uint)spellCraftCost;
        player.SendServerMessage($"Lost {spellCraftCost} GP for crafting {targetItem.Name}.");
    }
    
    private void AddClassRestrictions()
    {
        foreach (NwClass c in NwRuleset.Classes.Where(c => c.IsPlayerClass))
            if (_spell.GetSpellLevelForClass(c) != 255)
                targetItem.AddItemProperty(ItemProperty.LimitUseByClass(c), EffectDuration.Permanent);
    }

    private async Task ScribeScroll(NwCreature caster, int spellPropId)
    {
        targetItem.BaseItem = NwBaseItem.FromItemType(BaseItemType.SpellScroll)!;
        
        targetItem.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse), 
            EffectDuration.Permanent);

        AddClassRestrictions();
        
        Location? casterLocation = caster.Location;
        if (casterLocation == null) return;

        NwItem scribedScroll = targetItem.Clone(casterLocation);
        
        targetItem.Destroy();

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));
        
        SetScrollNameAndDescription(scribedScroll);
        
        caster.AcquireItem(scribedScroll);
    }

    private int CalculateScribeCost(int spellPropCl, int spellInnateLevel) =>
        spellPropCl * spellInnateLevel * 25 * targetItem.StackSize;
    
    private void SetScrollNameAndDescription(NwItem scribedScroll)
    {
        scribedScroll.Name = _spell.Name.ToString();
        scribedScroll.Description = _spell.Description.ToString();
    }
    
    private void CraftWand(NwCreature caster, int spellPropId, int craftWandCost, int casterLevel)
    {
        targetItem.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.ChargePerUse1), 
            EffectDuration.Permanent);

        targetItem.ItemCharges = casterLevel + 20;
        // Apparently wands do some hardcoded voodoo so check ingame what happens here
        
        caster.Gold -= (uint)craftWandCost;
    }
    
    private static int CalculateCraftWandCost(int spellPropCl, int spellInnateLevel) =>
        spellPropCl * spellInnateLevel * 750;
    
    private async Task BrewPotion(NwCreature caster, int spellPropId, int brewPotionCost)
    {
        targetItem.Destroy();

        await caster.WaitForObjectContext();
        NwItem? brewedPotion = await NwItem.Create(MagicPotion, caster, targetItem.StackSize);
        
        targetItem.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse), 
            EffectDuration.Permanent);
        
        // Apparently potions do some hardcoded voodoo so check ingame what happens here
        
        caster.Gold -= (uint)brewPotionCost;
    }
    
    private int CalculateBrewPotionCost(int spellPropCl, int spellInnateLevel) =>
        (int)(spellPropCl * spellInnateLevel * 12.5 * targetItem.StackSize);

    private static void ApplySpellCraftFailVfx(NwCreature caster)
    {
        caster.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect((VfxType)SpellFailVfx));
    }
    
    private static void ApplySpellCraftSuccessVfx(NwCreature caster)
    {
        caster.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfPwstun, fScale: 0.5f));
    }
    
    private (int SpellPropId, int SpellPropCl)? GetSpellPropIdAndCl(TwoDimArray spellPropTable)
    {
        int spellId = _spell.Id;

        List<int> spellPropRows = [];
        for (int i = 0; i < spellPropTable.RowCount; i++)
        {
            if (spellPropTable.GetInt(i, "SpellIndex") == spellId)
                spellPropRows.Add(i);
        }

        if (spellPropRows.Count == 0)
            return null;
        
        List<(int SpellPropId, int SpellPropCl)> spellPropIdAndClList = [];
        foreach (int row in spellPropRows)
        {
            int? spellPropCl = spellPropTable.GetInt(row, "CasterLvl");
            if (spellPropCl.HasValue)
                spellPropIdAndClList.Add((row, spellPropCl.Value));
        }

        if (spellPropIdAndClList.Count == 0) return null;

        (int SpellPropId, int SpellPropCl) spellPropIdAndCl = 
            spellPropIdAndClList.MaxBy(entry => entry.SpellPropCl);

        return spellPropIdAndCl;
    }
}