using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.SkillBonus)]
public class SkillBonusValidator : IValidationRule
{

    private readonly List<string> _personalSkills = new List<string>
    {
        "Appraise",
        "Bluff",
        "Craft Armor",
        "Craft Weapon",
        "Craft Trap",
        "Intimidate",
        "Lore",
        "Persuade",
        "Pick Pocket",
        "Open Lock",
        "Ride",
        "Tumble",
        "Use Magic Device",
    };

    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        SkillBonus skillBonus = new(incoming);
        LogManager.GetCurrentClassLogger().Info($"{skillBonus.Skill} {skillBonus.Bonus}");

        bool isPersonalSkill = _personalSkills.Contains(skillBonus.Skill);
        if (isPersonalSkill)
        {
            // Logic for personal skills
        }

        return isPersonalSkill
            ? ValidatePersonalSkill(skillBonus, itemProperties, changelistProperties)
            : ValidateBeneficialSkill(skillBonus, itemProperties, changelistProperties);
    }

    private ValidationResult ValidatePersonalSkill(SkillBonus skillBonus, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;

        // First, let's find any existing personal skills in the changelist that haven't been removed
        List<SkillBonus> skillsInChangelist = changelistProperties
            .Where(e => e.BasePropertyType == ItemPropertyType.SkillBonus &&
                        e.State != ChangeListModel.ChangeState.Removed)
            .Select(p => SkillBonus.FromProperty(p.Property))
            .Where(s => _personalSkills.Contains(s.Skill))
            .ToList();

        // Next, let's find any existing personal skills in the item properties
        List<SkillBonus> skillsInItem = itemProperties
            .Where(e => e.Property.PropertyType == ItemPropertyType.SkillBonus)
            .Select(SkillBonus.FromProperty)
            .Where(s => _personalSkills.Contains(s.Skill))
            .ToList();

        // Combine the two lists
        List<SkillBonus> allPersonalSkills = skillsInChangelist.Concat(skillsInItem).ToList();

        // Check if the incoming skill is already in the item properties or changelist
        bool anySkill = allPersonalSkills.Any(x => x.Skill == skillBonus.Skill);

        result = anySkill ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
        error = anySkill ? "Personal skill already exists on this item." : string.Empty;

        // Check if any of the existing skills are +5 since there may only be one
        bool hasMaxSkill = allPersonalSkills.Any(x => x.Bonus == 5) && skillBonus.Bonus == 5;

        result = hasMaxSkill ? ValidationEnum.LimitReached : result;
        error = hasMaxSkill ? "Free personal skill bonus limit reached." : error;

        return new ValidationResult
        {
            Enum = result,
            ErrorMessage = error
        };
    }

    private ValidationResult ValidateBeneficialSkill(SkillBonus skillBonus, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;


        // Get all the skills in the changelist that haven't been removed
        List<SkillBonus> skillsInChangelist = changelistProperties
            .Where(e => e.BasePropertyType == ItemPropertyType.SkillBonus &&
                        e.State != ChangeListModel.ChangeState.Removed)
            .Select(p => SkillBonus.FromProperty(p.Property))
            .Where(s => !_personalSkills.Contains(s.Skill))
            .ToList();

        // Get all the skills in the item properties
        List<SkillBonus> skillsInItem = itemProperties
            .Where(e => e.Property.PropertyType == ItemPropertyType.SkillBonus)
            .Select(SkillBonus.FromProperty)
            .Where(s => !_personalSkills.Contains(s.Skill))
            .ToList();

        // Combine the two lists
        List<SkillBonus> allSkills = skillsInChangelist.Concat(skillsInItem).ToList();

        // Check if the incoming skill is already in the item properties or changelist
        bool anySkill = allSkills.Any(x => x.Skill == skillBonus.Skill);

        result = anySkill ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;
        error = anySkill ? "Skill already exists on this item." : string.Empty;

        return new ValidationResult
        {
            Enum = result,
            ErrorMessage = error
        };
    }

    private class SkillBonus
    {
        public static SkillBonus FromProperty(ItemProperty itemProperty)
        {
            return new SkillBonus(itemProperty);
        }

        public SkillBonus(ItemProperty itemProperty)
        {
            ItemPropertyModel model = new()
            {
                Property = itemProperty,
                GoldCost = 0
            };

            Skill = model.SubTypeName;
            Bonus = int.Parse(model.PropertyBonus);
        }

        public string Skill { get; set; }
        public int Bonus { get; set; }
    }
}