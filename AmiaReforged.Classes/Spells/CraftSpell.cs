using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public class CraftSpell(SpellEvents.OnSpellCast eventData, NwItem targetItem)
{
    private static readonly TwoDimArray? SpellPropTable = NwGameTables.GetTable("iprp_spells");
    private const int SpellFailVfx = 292;

    private readonly bool _isEmptyScroll = targetItem.BaseItem.ItemType == BaseItemType.BlankScroll;
    private readonly bool _isEmptyWand = targetItem.BaseItem.ItemType == BaseItemType.BlankWand;
    private readonly bool _isEmptyPotion = targetItem.BaseItem.ItemType == BaseItemType.BlankPotion;

    private readonly NwSpell _spell = eventData.Spell;

    private const byte PotionColorYellow = 0;
    private const byte PotionColorGreen = 1;
    private const byte PotionColorOrange = 2;
    private const byte PotionColorRed = 3;
    private const byte PotionColorViolet = 4;
    private const byte PotionColorBlack = 6;
    private const byte PotionColorDarkViolet = 7;
    private const byte PotionColorWhite = 8;
    private const byte PotionColorBlue = 9;

    public void DoCraftSpell()
    {
        if (SpellPropTable == null) return;
        if (eventData.Caster == null) return;
        
        NwCreature caster = (NwCreature)eventData.Caster;

        if (!caster.IsPlayerControlled(out NwPlayer? player)) return;
        
        if (!(_isEmptyScroll || _isEmptyWand || _isEmptyPotion)) return;
        
        if (caster.Inventory.Items.All(item => item != targetItem))
        {
            player.SendServerMessage($"Spell craft failed! {targetItem.Name} must be in your inventory.");
            ApplySpellCraftFailVfx(caster);
            return;
        }

        if (targetItem.HasItemProperty(ItemPropertyType.CastSpell))
        {
            player.SendServerMessage($"Spell craft failed! {targetItem.Name} has already been spell crafted.");
            ApplySpellCraftFailVfx(caster);
            return;
        }
        
        if (eventData.Item != null)
        {
            player.SendServerMessage("Spell craft failed! You can only craft spells when casting from a spellbook.");
            ApplySpellCraftFailVfx(caster);
            return;
        }
        
        (int SpellPropId, int SpellPropCl)? spellPropIdAndCl = GetSpellPropIdAndCl(SpellPropTable);

        if (spellPropIdAndCl == null)
        {
            player.SendServerMessage("Spell craft failed! There is no item property associated with this spell.");
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

            ScribeScroll(caster, spellPropId);
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

            BrewPotion(spellPropId);
            ChargeForSpellCraft(player, caster, brewPotionCost);
            ApplySpellCraftSuccessVfx(caster);
        }
        
    }

    private void ChargeForSpellCraft(NwPlayer player, NwCreature caster, int spellCraftCost)
    {
        caster.Gold -= (uint)spellCraftCost;
        player.SendServerMessage($"Lost {spellCraftCost} GP.");
    }
    
    private void AddClassRestrictions(NwItem item)
    {
        foreach (NwClass c in NwRuleset.Classes.Where(c => c.IsPlayerClass))
            if (_spell.GetSpellLevelForClass(c) != 255)
                item.AddItemProperty(ItemProperty.LimitUseByClass(c), EffectDuration.Permanent);
    }

    private void ScribeScroll(NwCreature caster, int spellPropId)
    {
        targetItem.BaseItem = NwBaseItem.FromItemType(BaseItemType.SpellScroll)!;
        
        NwModule.Instance.MoveObjectToLimbo(targetItem);
        
        targetItem.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse), 
            EffectDuration.Permanent);

        AddClassRestrictions(targetItem);
        
        SetScrollNameAndDescription(targetItem);
        
        caster.AcquireItem(targetItem);
    }

    private int CalculateScribeCost(int spellPropCl, int spellInnateLevel) =>
        spellInnateLevel == 1 
            ? spellPropCl * 1 * 25 * targetItem.StackSize 
            : spellPropCl * spellInnateLevel * 25 * targetItem.StackSize;
    
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
        spellInnateLevel == 1 ? spellPropCl * 1 * 750 : spellPropCl * spellInnateLevel * 750;
    
    private void BrewPotion(int spellPropId)
    {
        targetItem.BaseItem = NwBaseItem.FromItemType(BaseItemType.EnchantedPotion)!;
        
        targetItem.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse), 
            EffectDuration.Permanent);
        
        SetPotionNameAndDescription(targetItem);

        targetItem.Appearance.ChangeAppearance(appearance => 
            appearance.SetWeaponColor(ItemAppearanceWeaponColor.Bottom, GetPotionColor()));
    }

    private void SetPotionNameAndDescription(NwItem brewedPotion)
    {
        brewedPotion.Name = "Potion of "+_spell.Name;
        brewedPotion.Description = _spell.Description.ToString();
    }

    private byte GetPotionColor()
    {
        return _spell.SpellSchool switch
        {
            SpellSchool.Abjuration => PotionColorYellow,
            SpellSchool.Conjuration => PotionColorViolet,
            SpellSchool.Divination => PotionColorBlue,
            SpellSchool.Enchantment => PotionColorDarkViolet,
            SpellSchool.Evocation => PotionColorRed,
            SpellSchool.Illusion => PotionColorGreen,
            SpellSchool.Necromancy => PotionColorBlack,
            SpellSchool.Transmutation => PotionColorOrange,
            _ => PotionColorWhite
        };
    }

    private int CalculateBrewPotionCost(int spellPropCl, int spellInnateLevel) =>
        (int)(spellInnateLevel == 0 
            ? spellPropCl * 1 * 12.5 * targetItem.StackSize 
            : spellPropCl * spellInnateLevel * 12.5 * targetItem.StackSize); 

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