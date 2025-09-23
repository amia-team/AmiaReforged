using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Spells;

public class CraftSpell(OnSpellCast eventData, NwSpell spell, NwItem targetItem)
{
    private static readonly TwoDimArray? SpellPropTable = NwGameTables.GetTable("iprp_spells");

    private const int SpellFailVfx = 292;

    private const string PotionPrefix = "brewpot_";
    private const string WandPrefix = "craftwand_";

    private const string PotionUniversal = "x2_it_pcpotion";
    private const string WandUniversal = "x2_it_pcwand";

    private readonly Dictionary<SpellSchool, string> _schoolSuffixes = new()
    {
        { SpellSchool.Abjuration, "abju" },
        { SpellSchool.Conjuration, "conj" },
        { SpellSchool.Divination, "divi" },
        { SpellSchool.Enchantment, "ench" },
        { SpellSchool.Evocation, "evoc" },
        { SpellSchool.Illusion, "illu" },
        { SpellSchool.Necromancy, "necr" },
        { SpellSchool.Transmutation, "tran" }
    };

    private enum SpellCraftResult
    {
        Success,
        NoFeat,
        NotEnoughGold,
        WrongSpellLevel,
        HostileSpell
    }

    private readonly string _spellName = spell.MasterSpell?.Name.ToString() ?? spell.Name.ToString();
    private readonly string _spellDescription = spell.MasterSpell?.Description.ToString() ?? spell.Description.ToString();

    public void DoCraftSpell()
    {
        if (SpellPropTable == null) return;
        if (eventData.Caster is not NwCreature caster) return;
        if (!caster.IsPlayerControlled(out NwPlayer? player)) return;
        if (eventData.Item != null) return;

        if (targetItem.BaseItem.ItemType is not
            (BaseItemType.BlankScroll or BaseItemType.BlankWand or BaseItemType.BlankPotion)) return;

        if (caster.Inventory.Items.All(item => item != targetItem))
        {
            player.SendServerMessage($"Spell craft failed! {targetItem.Name} must be in your inventory.");
            ApplySpellCraftFailVfx(caster);
            eventData.PreventSpellCast = true;
            return;
        }

        if (targetItem.HasItemProperty(ItemPropertyType.CastSpell))
        {
            player.SendServerMessage($"Spell craft failed! {targetItem.Name} has already been spell crafted.");
            ApplySpellCraftFailVfx(caster);
            eventData.PreventSpellCast = true;
            return;
        }

        (int SpellPropId, int SpellPropCl)? spellPropIdAndCl = GetSpellPropIdAndCl(SpellPropTable);

        if (spellPropIdAndCl == null)
        {
            player.SendServerMessage("Spell craft failed! There is no item property associated with this spell.");
            ApplySpellCraftFailVfx(caster);
            eventData.PreventSpellCast = true;
            return;
        }

        int spellPropId = spellPropIdAndCl.Value.SpellPropId;
        int spellPropCl = spellPropIdAndCl.Value.SpellPropCl;

        switch (targetItem.BaseItem.ItemType)
        {
            case BaseItemType.BlankScroll:
                SpellCraftResult scrollResult = ValidateScribeScroll(caster, spell.InnateSpellLevel, spellPropCl, out int scribeCost);
                HandleCraftingResult(scrollResult, player, caster, spell.InnateSpellLevel, scribeCost,9);

                if (scrollResult == SpellCraftResult.Success)
                {
                    _ = ScribeScroll(caster, spellPropId);
                    ChargeForSpellCraft(player, caster, scribeCost);
                }
                break;

            case BaseItemType.BlankWand:
                SpellCraftResult wandResult = ValidateCraftWand(caster, spell.InnateSpellLevel, out int wandCost);
                HandleCraftingResult(wandResult, player, caster, spell.InnateSpellLevel, wandCost, 4);

                if (wandResult == SpellCraftResult.Success)
                {
                    byte casterLevel = caster.Classes[eventData.ClassIndex].Level;
                    CraftWand(caster, spellPropId, casterLevel);
                    ChargeForSpellCraft(player, caster, wandCost);
                }
                break;

            case BaseItemType.BlankPotion:
                SpellCraftResult potionResult = ValidateBrewPotion(caster, spell.InnateSpellLevel, spell.IsHostileSpell, out int potionCost);
                HandleCraftingResult(potionResult, player, caster, spell.InnateSpellLevel, potionCost, 3);

                if (potionResult == SpellCraftResult.Success)
                {
                    _ = BrewPotion(caster, spellPropId);
                    ChargeForSpellCraft(player, caster, potionCost);
                }
                break;
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
        {
            byte spellLevel = spell.MasterSpell?.GetSpellLevelForClass(c) ?? spell.GetSpellLevelForClass(c);

            if (spellLevel != 255)
                item.AddItemProperty(ItemProperty.LimitUseByClass(c), EffectDuration.Permanent);
        }

    }

    private async Task ScribeScroll(NwCreature caster, int spellPropId)
    {
        if (caster.Location == null) return;

        NwItem scribedScroll = targetItem.Clone(caster.Location);
        scribedScroll.StackSize = 1;

        if (targetItem.StackSize == 1)
            targetItem.Destroy();
        else
            targetItem.StackSize -= 1;

        NwBaseItem? spellScroll = NwBaseItem.FromItemType(BaseItemType.SpellScroll);
        if (spellScroll == null) return;

        scribedScroll.BaseItem = spellScroll;

        scribedScroll.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse),
            EffectDuration.Permanent);

        scribedScroll.Name = _spellName;
        scribedScroll.Description = _spellDescription;

        AddClassRestrictions(scribedScroll);

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));

        caster.AcquireItem(scribedScroll);
    }

    private int CalculateScribeCost(int spellPropCl, int spellInnateLevel) =>
        spellInnateLevel == 0
            ? spellPropCl * 1 * 25
            : spellPropCl * spellInnateLevel * 25;

    private void CraftWand(NwCreature caster, int spellPropId, byte casterLevel)
    {
        if (caster.Location == null) return;

        NwItem? craftedWand = NwItem.Create(GetColoredItem(), caster.Location);
        if (craftedWand == null) return;

        if (targetItem.StackSize == 1)
            targetItem.Destroy();
        else
            targetItem.StackSize--;


        craftedWand.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.ChargePerUse1),
            EffectDuration.Permanent);

        craftedWand.ItemCharges = casterLevel + 20;

        craftedWand.Name = "Wand of "+_spellName;
        craftedWand.Description = _spellDescription;

        AddClassRestrictions(craftedWand);

        caster.AcquireItem(craftedWand);
    }

    private string GetColoredItem()
    {
        string prefix = targetItem.BaseItem.ItemType == BaseItemType.BlankPotion ? PotionPrefix : WandPrefix;
        string universalItem = targetItem.BaseItem.ItemType == BaseItemType.BlankPotion ? PotionUniversal : WandUniversal;

        return _schoolSuffixes.TryGetValue(spell.SpellSchool, out string? suffix) ? $"{prefix}{suffix}" : universalItem;
    }

    private static int CalculateCraftWandCost(int spellInnateLevel, int casterLevel) =>
        (int) (25 * Math.Pow(2, spellInnateLevel) * (20 + casterLevel));

    private async Task BrewPotion(NwCreature caster, int spellPropId)
    {
        int stackSize = targetItem.StackSize;

        if (caster.Location == null) return;

        NwItem? brewedPotion = NwItem.Create(GetColoredItem(), caster.Location, stackSize: stackSize);
        if (brewedPotion == null) return;

        targetItem.Destroy();

        NwBaseItem? enchantedPotion = NwBaseItem.FromItemType(BaseItemType.EnchantedPotion);
        if (enchantedPotion == null) return;

        brewedPotion.BaseItem = enchantedPotion;

        brewedPotion.AddItemProperty(ItemProperty.CastSpell((IPCastSpell)spellPropId, IPCastSpellNumUses.SingleUse),
            EffectDuration.Permanent);

        brewedPotion.Name = "Potion of "+_spellName;
        brewedPotion.Description = _spellDescription;

        await NwTask.Delay(TimeSpan.FromMilliseconds(1));

        caster.AcquireItem(brewedPotion);
    }

    private int CalculateBrewPotionCost(int spellInnateLevel) =>
        (int)(25 * Math.Pow(2, spellInnateLevel) * targetItem.StackSize);

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

        int spellId = spell.MasterSpell?.Id ?? spell.Id;

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

    private SpellCraftResult ValidateScribeScroll(NwCreature caster, int spellLevel, int spellPropCl, out int cost)
    {
        cost = CalculateScribeCost(spellPropCl, spellLevel);
        if (!caster.KnowsFeat(Feat.ScribeScroll!)) return SpellCraftResult.NoFeat;
        if (spellLevel > 9) return SpellCraftResult.WrongSpellLevel;
        if (caster.Gold < cost) return SpellCraftResult.NotEnoughGold;
        return SpellCraftResult.Success;
    }

    private SpellCraftResult ValidateCraftWand(NwCreature caster, int spellLevel, out int cost)
    {
        cost = CalculateCraftWandCost(spellLevel, caster.Classes[eventData.ClassIndex].Level);
        if (!caster.KnowsFeat(Feat.CraftWand!)) return SpellCraftResult.NoFeat;
        if (spellLevel > 4) return SpellCraftResult.WrongSpellLevel;
        if (caster.Gold < cost) return SpellCraftResult.NotEnoughGold;
        return SpellCraftResult.Success;
    }

    private SpellCraftResult ValidateBrewPotion(NwCreature caster, int spellLevel, bool isHostile, out int cost)
    {
        cost = CalculateBrewPotionCost(spellLevel);
        if (!caster.KnowsFeat(Feat.BrewPotion!)) return SpellCraftResult.NoFeat;
        if (spellLevel > 3) return SpellCraftResult.WrongSpellLevel;
        if (isHostile) return SpellCraftResult.HostileSpell;
        if (caster.Gold < cost) return SpellCraftResult.NotEnoughGold;
        return SpellCraftResult.Success;
    }

    private void HandleCraftingResult(SpellCraftResult result, NwPlayer player, NwCreature caster, int innateSpellLevel,
        int cost, int? maxSpellLevel = null)
    {
        switch (result)
        {
            case SpellCraftResult.NoFeat:
                player.SendServerMessage("Crafting failed! You don't know the required feat.");
                break;
            case SpellCraftResult.NotEnoughGold:
                player.SendServerMessage($"Crafting failed! You don't have enough gold. The cost is {cost} GP.");
                break;
            case SpellCraftResult.WrongSpellLevel:
                player.SendServerMessage($"Crafting failed! Innate spell level must be {maxSpellLevel} or lower. " +
                                         $"The innate level is {innateSpellLevel}.");
                break;
            case SpellCraftResult.HostileSpell:
                player.SendServerMessage("Crafting failed! You cannot craft an item from a hostile spell.");
                break;
            case SpellCraftResult.Success:
                ApplySpellCraftSuccessVfx(caster);
                return;
        }

        ApplySpellCraftFailVfx(caster);
        eventData.PreventSpellCast = true;
    }
}
