using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands;

public class CreatureData
{
    public string TemplateResRef { get; set; }
    public string FullName { get; set; }
    public List<string> FoundInAreas { get; set; } = new();
    public string FirstClassName { get; set; }
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


    public ItemData RightHand { get; set; }
    public ItemData LeftHand { get; set; }
    public ItemData Gloves { get; set; }
    public ItemData Armor { get; set; }
    public ItemData Helmet { get; set; }
    public ItemData Ring1 { get; set; }
    public ItemData Ring2 { get; set; }
    public ItemData Neck { get; set; }
    public ItemData Boots { get; set; }
    public ItemData Cloak { get; set; }
    
    public ItemData Arrows { get; set; }
    public ItemData Bullets { get; set; }
    public ItemData Bolts { get; set; }
    
    public ItemData CreatureHide { get; set; }
    public ItemData CreatureRightWeapon { get; set; }
    public ItemData CreatureLeftWeapon { get; set; }
    public ItemData CreatureBite { get; set; }
    
    
    public InventoryData Inventory { get; set; }
    public CreatureEventData Events { get; set; }
    public IEnumerable<SpecialAbilityData> SpecialAbilities { get; set; }
    public IEnumerable<SpellPowerData> SpellAbilities { get; set; }
    public IEnumerable<FeatData> Feats { get; set; }
}

public class SkillData
{
    public string Name { get; set; }
    public int Value { get; set; }
}

public class FeatData
{
    public string Name { get; set; }
}

public class SpellPowerData
{
    public string Name { get; set; }
    public int CasterLevel { get; set; }
}

public class SpecialAbilityData
{
    public string Name { get; set; }
    public byte CasterLevel { get; set; }
}

public class CreatureEventData
{
    public string OnBlocked { get; set; }
    public string OnUserDefined { get; set; }
    public string OnDeath { get; set; }
    public string OnDamaged { get; set; }
    public string OnConversation { get; set; }
    public string OnDisturbed { get; set; }
    public string OnHeartbeat { get; set; }
    public string OnCombatRoundEnd { get; set; }
    public string OnPhysicalAttacked { get; set; }
    public string OnPerception { get; set; }
    public string OnRested { get; set; }
    public string OnSpawn { get; set; }
    public string OnSpellCast { get; set; }
}

public class InventoryData
{
    public IEnumerable<ItemData> Items { get; set; }
}

public class ItemData
{
    public string Name { get; set; }
    public string Type { get; set; }
    public IEnumerable<ItemPropertyData> ItemProperties { get; set; }
}

public class ItemPropertyData
{
    public string ItemProperty { get; set; } 
}