namespace AmiaReforged.PwEngine.Features.Module;

public class CreatureData
{
    public string? TemplateResRef { get; set; }
    public string? FullName { get; set; }
    public List<string> FoundInAreas { get; set; } = new();
    public string? FirstClassName { get; set; }
    public string? SecondClassName { get; set; }
    public string? ThirdClassName { get; set; }
    public int Level { get; set; }

    public float ChallengeRating { get; set; }

    public List<SkillData> Skills { get; set; } = new();

    public int HitPoints { get; set; }
    public int FortitudeSave { get; set; }
    public int ReflexSave { get; set; }
    public int WillSave { get; set; }

    public int Strength { get; set; }
    public int RawStrength { get; set; }
    public int StrengthModifier { get; set; }

    public int Dexterity { get; set; }
    public int RawDexterity { get; set; }
    public int DexterityModifier { get; set; }

    public int Constitution { get; set; }
    public int RawConstitution { get; set; }
    public int ConstitutionModifier { get; set; }

    public int Intelligence { get; set; }
    public int RawIntelligence { get; set; }
    public int IntelligenceModifier { get; set; }

    public int Wisdom { get; set; }
    public int RawWisdom { get; set; }
    public int WisdomModifier { get; set; }

    public int Charisma { get; set; }
    public int RawCharisma { get; set; }
    public int CharismaModifier { get; set; }

    public int AttackBonus { get; set; }
    public int BaseAttackBonus { get; set; }
    public int AttacksPerRound { get; set; }
    public int ArmorClass { get; set; }
    public int SpellResistance { get; set; }


    public ItemData? RightHand { get; set; }
    public ItemData? LeftHand { get; set; }
    public ItemData? Gloves { get; set; }
    public ItemData? Armor { get; set; }
    public ItemData? Helmet { get; set; }
    public ItemData? Ring1 { get; set; }
    public ItemData? Ring2 { get; set; }
    public ItemData? Neck { get; set; }
    public ItemData? Boots { get; set; }
    public ItemData? Cloak { get; set; }

    public ItemData? Arrows { get; set; }
    public ItemData? Bullets { get; set; }
    public ItemData? Bolts { get; set; }

    public ItemData? CreatureHide { get; set; }
    public ItemData? CreatureRightWeapon { get; set; }
    public ItemData? CreatureLeftWeapon { get; set; }
    public ItemData? CreatureBite { get; set; }


    public InventoryData? Inventory { get; set; }
    public CreatureEventData? Events { get; set; }
    public IEnumerable<SpecialAbilityData>? SpecialAbilities { get; set; }
    public IEnumerable<SpellPowerData>? SpellAbilities { get; set; }
    public IEnumerable<FeatData>? Feats { get; set; }
}
