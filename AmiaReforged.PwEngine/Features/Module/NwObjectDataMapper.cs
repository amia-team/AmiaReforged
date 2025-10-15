using AmiaReforged.PwEngine.Features.NwObjectHelpers;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Module;

[ServiceBinding(typeof(NwObjectDataMapper))]
public class NwObjectDataMapper
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly BlueprintManager _manager;

    public NwObjectDataMapper(BlueprintManager manager)
    {
        _manager = manager;
    }

    public CreatureData FromCreature(NwCreature creature)
    {
        CreatureData data = new()
        {
            TemplateResRef = creature.ResRef,
            FullName = creature.Name,
            FirstClassName = creature.Classes[0].Class.Name.ToString(),
            SecondClassName = creature.Classes.Count > 1 ? creature.Classes[1].Class.Name.ToString() : null,
            ThirdClassName = creature.Classes.Count > 2 ? creature.Classes[2].Class.Name.ToString() : null,
            Level = creature.Level,
            Skills = MapFromSkills(creature),

            HitPoints = creature.MaxHP,
            FortitudeSave = creature.GetSavingThrow(SavingThrow.Fortitude),
            ReflexSave = creature.GetSavingThrow(SavingThrow.Reflex),
            WillSave = creature.GetSavingThrow(SavingThrow.Will),
            ArmorClass = creature.AC,

            Strength = creature.GetAbilityScore(Ability.Strength),
            RawStrength = creature.GetRawAbilityScore(Ability.Strength),
            StrengthModifier = creature.GetAbilityModifier(Ability.Strength),

            Dexterity = creature.GetAbilityScore(Ability.Dexterity),
            RawDexterity = creature.GetRawAbilityScore(Ability.Dexterity),
            DexterityModifier = creature.GetAbilityModifier(Ability.Dexterity),

            Constitution = creature.GetAbilityScore(Ability.Constitution),
            RawConstitution = creature.GetRawAbilityScore(Ability.Constitution),
            ConstitutionModifier = creature.GetAbilityModifier(Ability.Constitution),

            Intelligence = creature.GetAbilityScore(Ability.Intelligence),
            RawIntelligence = creature.GetRawAbilityScore(Ability.Intelligence),
            IntelligenceModifier = creature.GetAbilityModifier(Ability.Intelligence),

            Wisdom = creature.GetAbilityScore(Ability.Wisdom),
            RawWisdom = creature.GetRawAbilityScore(Ability.Wisdom),
            WisdomModifier = creature.GetAbilityModifier(Ability.Wisdom),

            Charisma = creature.GetAbilityScore(Ability.Charisma),
            RawCharisma = creature.GetRawAbilityScore(Ability.Charisma),
            CharismaModifier = creature.GetAbilityModifier(Ability.Charisma),

            AttackBonus = creature.GetAttackBonus(),
            BaseAttackBonus = creature.BaseAttackBonus,
            AttacksPerRound = creature.BaseAttackCount,
            SpellResistance = creature.SpellResistance,

            RightHand = MapFromItem(creature.GetItemInSlot(InventorySlot.RightHand)),
            LeftHand = MapFromItem(creature.GetItemInSlot(InventorySlot.LeftHand)),
            Gloves = MapFromItem(creature.GetItemInSlot(InventorySlot.Arms)),
            Armor = MapFromItem(creature.GetItemInSlot(InventorySlot.Chest)),
            Helmet = MapFromItem(creature.GetItemInSlot(InventorySlot.Head)),
            Ring1 = MapFromItem(creature.GetItemInSlot(InventorySlot.RightRing)),
            Ring2 = MapFromItem(creature.GetItemInSlot(InventorySlot.LeftRing)),
            Neck = MapFromItem(creature.GetItemInSlot(InventorySlot.Neck)),
            Boots = MapFromItem(creature.GetItemInSlot(InventorySlot.Boots)),
            Cloak = MapFromItem(creature.GetItemInSlot(InventorySlot.Cloak)),
            Arrows = MapFromItem(creature.GetItemInSlot(InventorySlot.Arrows)),
            Bullets = MapFromItem(creature.GetItemInSlot(InventorySlot.Bullets)),
            Bolts = MapFromItem(creature.GetItemInSlot(InventorySlot.Bolts)),
            CreatureHide = MapFromItem(creature.GetItemInSlot(InventorySlot.CreatureSkin)),
            CreatureRightWeapon = MapFromItem(creature.GetItemInSlot(InventorySlot.CreatureRightWeapon)),
            CreatureLeftWeapon = MapFromItem(creature.GetItemInSlot(InventorySlot.CreatureLeftWeapon)),
            CreatureBite = MapFromItem(creature.GetItemInSlot(InventorySlot.CreatureBiteWeapon)),

            SpecialAbilities = MapFromSpecialAbilities(creature.SpecialAbilities),
            SpellAbilities = MapFromSpellAbilities(creature.SpellAbilities),

            Events = MapFromCreatureEvents(creature),

            ChallengeRating = creature.ChallengeRating,
            Inventory = MapFromInventory(creature.Inventory),
            Feats = MapFromFeats(creature)
        };

        return data;
    }

    private List<SkillData> MapFromSkills(NwCreature creature)
    {
        List<Skill> skills =
        [
            Skill.Concentration,
            Skill.Discipline,
            Skill.Heal,
            Skill.Hide,
            Skill.MoveSilently,
            Skill.Parry,
            Skill.Persuade,
            Skill.Spot,
            Skill.Listen,
            Skill.Spellcraft
        ];

        return skills.Select(skill => new SkillData
        {
            Name = skill.ToString(),
            Value = creature.GetSkillRank(skill!)
        }).ToList();
    }

    private IEnumerable<FeatData> MapFromFeats(NwCreature creature)
    {
        return creature.Feats.Select(MapFromFeat).ToList();
    }

    private FeatData MapFromFeat(NwFeat feat)
    {
        FeatData data = new()
        {
            Name = feat.Name.ToString(),
        };

        return data;
    }

    private CreatureEventData MapFromCreatureEvents(NwCreature creature)
    {
        CreatureEventData data = new()
        {
            OnBlocked = creature.GetEventScript(EventScriptType.CreatureOnBlockedByDoor),
            OnDamaged = creature.GetEventScript(EventScriptType.CreatureOnDamaged),
            OnDeath = creature.GetEventScript(EventScriptType.CreatureOnDeath),
            OnConversation = creature.GetEventScript(EventScriptType.CreatureOnDialogue),
            OnDisturbed = creature.GetEventScript(EventScriptType.CreatureOnDisturbed),
            OnCombatRoundEnd = creature.GetEventScript(EventScriptType.CreatureOnEndCombatRound),
            OnHeartbeat = creature.GetEventScript(EventScriptType.CreatureOnHeartbeat),
            OnPhysicalAttacked = creature.GetEventScript(EventScriptType.CreatureOnMeleeAttacked),
            OnPerception = creature.GetEventScript(EventScriptType.CreatureOnNotice),
            OnRested = creature.GetEventScript(EventScriptType.CreatureOnRested),
            OnSpawn = creature.GetEventScript(EventScriptType.CreatureOnSpawnIn),
            OnSpellCast = creature.GetEventScript(EventScriptType.CreatureOnSpellCastAt),
            OnUserDefined = creature.GetEventScript(EventScriptType.CreatureOnUserDefinedEvent),
        };

        return data;
    }

    private IEnumerable<SpellPowerData> MapFromSpellAbilities(IEnumerable<CreatureSpellAbility> creatureSpellAbilities)
    {
        return creatureSpellAbilities.Select(MapFromSpellAbility).ToList();
    }

    private SpellPowerData MapFromSpellAbility(CreatureSpellAbility arg)
    {
        SpellPowerData data = new()
        {
            Name = arg.Spell.Name.ToString(),
            CasterLevel = arg.CasterLevel
        };

        return data;
    }

    private IEnumerable<SpecialAbilityData> MapFromSpecialAbilities(
        IReadOnlyList<SpecialAbility> creatureSpecialAbilities)
    {
        return creatureSpecialAbilities.Select(MapFromSpecialAbility).ToList();
    }

    private SpecialAbilityData MapFromSpecialAbility(SpecialAbility ability)
    {
        SpecialAbilityData data = new()
        {
            Name = ability.Spell.Name.ToString(),
            CasterLevel = ability.CasterLevel
        };

        return data;
    }

    private InventoryData MapFromInventory(Inventory creatureInventory)
    {
        List<ItemData> items = creatureInventory.Items.Select(MapFromItem).ToList();

        return new InventoryData
        {
            Items = items
        };
    }

    private ItemData MapFromItem(NwItem? itm)
    {
        if (itm is null) return new ItemData();

        ItemData data = new()
        {
            Name = itm.Name,
            Type = itm.BaseItem.ItemType.ToString(),
            ItemProperties = itm.ItemProperties.Select(MapFromItemProperty).ToList()
        };

        return data;
    }

    private ItemPropertyData MapFromItemProperty(ItemProperty prop)
    {
        ItemPropertyData data = new()
        {
            ItemProperty = ItemPropertyHelper.FullPropertyDescription(prop)
        };

        return data;
    }
}
