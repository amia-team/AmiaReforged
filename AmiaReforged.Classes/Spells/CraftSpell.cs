using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public class CraftSpell(OnSpellCast eventData, NwSpell spell, NwItem targetItem)
{
    private static readonly TwoDimArray? SpellPropTable = NwGameTables.GetTable("iprp_spells");
    private const int SpellFailVfx = 292;

    private readonly bool _isEmptyScroll = targetItem.BaseItem.ItemType == BaseItemType.BlankScroll;
    private readonly bool _isEmptyWand = targetItem.BaseItem.ItemType == BaseItemType.BlankWand;
    private readonly bool _isEmptyPotion = targetItem.BaseItem.ItemType == BaseItemType.BlankPotion;

    private const string PotionAbjuration = "brewpot_abju";
    private const string PotionConjuration = "brewpot_conj";
    private const string PotionDivination = "brewpot_divi";
    private const string PotionEnchantment = "brewpot_ench";
    private const string PotionEvocation = "brewpot_evoc";
    private const string PotionIllusion = "brewpot_illu";
    private const string PotionNecromancy = "brewpot_necr";
    private const string PotionTransmutation = "brewpot_tran";
    private const string PotionUniversal = "x2_it_pcpotion";

    private const string WandAbjuration = "craftwand_abju";
    private const string WandConjuration = "craftwand_conj";
    private const string WandDivination = "craftwand_divi";
    private const string WandEnchantment = "craftwand_ench";
    private const string WandEvocation = "craftwand_evoc";
    private const string WandIllusion = "craftwand_illu";
    private const string WandNecromancy = "craftwand_nec";
    private const string WandTransmutation = "craftwand_tran";
    private const string WandUniversal = "x2_it_pcwand";

    private const string SpellScroll = "x2_it_spdvscr201";

    public void DoCraftSpell()
    {
        if (SpellPropTable == null) return;
        if (eventData.Caster is not NwCreature caster) return;
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
        int spellInnateLevel = spell.InnateSpellLevel;

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
                    ($"Craft wand failed! Innate spell level must be 4 or lower. The innate level of this spell is {spellInnateLevel}.");
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

            byte casterLevel = caster.Classes[eventData.ClassIndex].Level;

            CraftWand(caster, spellPropId, casterLevel);
            ChargeForSpellCraft(player, caster, craftWandCost);
            ApplySpellCraftSuccessVfx(caster);
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
                    ($"Brew potion failed! Innate spell level must be 3 or lower. The innate level of this spell is {spellInnateLevel}.");
                ApplySpellCraftFailVfx(caster);
                return;
            }

            if (spell.IsHostileSpell)
            {
                player.SendServerMessage
                    ("Brew potion failed! You cannot brew a potion from a hostile spell.");
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

            BrewPotion(caster, spellPropId);
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
            if (spell.GetSpellLevelForClass(c) != 255)
                item.AddItemProperty(ItemProperty.LimitUseByClass(c), EffectDuration.Permanent);
    }

    private async Task ScribeScroll(NwCreature caster, int spellPropId)
    {
        if (caster.Location == null) return;

        NwItem? scribedScroll = NwItem.Create(SpellScroll, caster.Location);
        if (scribedScroll == null) return;

        if (targetItem.StackSize == 1)
            targetItem.Destroy();
        else
            targetItem.StackSize -= 1;

        scribedScroll.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse),
            EffectDuration.Permanent);

        scribedScroll.Name = spell.Name.ToString();
        scribedScroll.Description = spell.Description.ToString();

        AddClassRestrictions(scribedScroll);

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));

        caster.AcquireItem(scribedScroll);
    }

    private int CalculateScribeCost(int spellPropCl, int spellInnateLevel) =>
        spellInnateLevel == 0
            ? spellPropCl * 1 * 25 * targetItem.StackSize
            : spellPropCl * spellInnateLevel * 25 * targetItem.StackSize;

    private void CraftWand(NwCreature caster, int spellPropId, byte casterLevel)
    {
        if (caster.Location == null) return;

        NwItem? craftedWand = NwItem.Create(GetWandBySchool(), caster.Location);
        if (craftedWand == null) return;

        if (targetItem.StackSize == 1)
            targetItem.Destroy();
        else
            targetItem.StackSize--;


        craftedWand.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.ChargePerUse1),
            EffectDuration.Permanent);

        craftedWand.ItemCharges = casterLevel + 20;

        craftedWand.Name = "Wand of "+spell.Name;
        craftedWand.Description = spell.Description.ToString();

        AddClassRestrictions(craftedWand);

        caster.AcquireItem(craftedWand);
    }

    private string GetWandBySchool()
    {
        return spell.SpellSchool switch
        {
            SpellSchool.Abjuration => WandAbjuration,
            SpellSchool.Conjuration => WandConjuration,
            SpellSchool.Divination => WandDivination,
            SpellSchool.Enchantment => WandEnchantment,
            SpellSchool.Evocation => WandEvocation,
            SpellSchool.Illusion => WandIllusion,
            SpellSchool.Necromancy => WandNecromancy,
            SpellSchool.Transmutation => WandTransmutation,
            _ => WandUniversal
        };
    }

    private static int CalculateCraftWandCost(int spellPropCl, int spellInnateLevel) =>
        spellInnateLevel == 0 ? spellPropCl * 1 * 750 : spellPropCl * spellInnateLevel * 750;

    private void BrewPotion(NwCreature caster, int spellPropId)
    {
        int stackSize = targetItem.StackSize;

        if (caster.Location == null) return;

        NwItem? brewedPotion = NwItem.Create(GetPotionBySchool(), caster.Location, stackSize: stackSize);
        if (brewedPotion == null) return;

        targetItem.Destroy();

        brewedPotion.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse),
            EffectDuration.Permanent);

        brewedPotion.Name = "Potion of "+spell.Name;
        brewedPotion.Description = spell.Description.ToString();

        caster.AcquireItem(brewedPotion);
    }

    private string GetPotionBySchool()
    {
        return spell.SpellSchool switch
        {
            SpellSchool.Abjuration => PotionAbjuration,
            SpellSchool.Conjuration => PotionConjuration,
            SpellSchool.Divination => PotionDivination,
            SpellSchool.Enchantment => PotionEnchantment,
            SpellSchool.Evocation => PotionEvocation,
            SpellSchool.Illusion => PotionIllusion,
            SpellSchool.Necromancy => PotionNecromancy,
            SpellSchool.Transmutation => PotionTransmutation,
            _ => PotionUniversal
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
        List<int> spellPropRows = [];
        for (int i = 0; i < spellPropTable.RowCount; i++)
        {
            if (spellPropTable.GetInt(i, "SpellIndex") == spell.Id)
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
