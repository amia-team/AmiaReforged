using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.SkillBonus)]
public class SkillBonusValidator : IValidationRule
{
    private readonly List<string> _personalSkills =
    [
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
        "Use Magic Device"
    ];

    private ValidationResult ValidatePersonalSkill(SkillBonus skillBonus, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
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

        // The skills in the changelist that were removed
        List<SkillBonus> removedSkills = changelistProperties
            .Where(e => e is
                { BasePropertyType: ItemPropertyType.SkillBonus, State: ChangeListModel.ChangeState.Removed })
            .Select(p => SkillBonus.FromProperty(p.Property))
            .Where(p => _personalSkills.Contains(p.Skill))
            .ToList();


        // Check if the incoming skill is already in the item properties or changelist
        bool onItem = skillsInItem.Any(x => x.Skill == skillBonus.Skill);
        bool wasNotRemoved = removedSkills.All(x => x.Skill != skillBonus.Skill);
        bool inChangelist = skillsInChangelist.Any(x => x.Skill == skillBonus.Skill);
        bool notRemoved = onItem && wasNotRemoved;
        bool anySkill = notRemoved || inChangelist;

        ValidationEnum result = anySkill ? ValidationEnum.CannotStackSameSubtype : ValidationEnum.Valid;

        // Check if any of the existing skills are +5 since there may only be one
        List<SkillBonus> addedFreebies = skillsInChangelist.Where(x => x.Bonus == 5).ToList();
        List<SkillBonus> existingFreebies = skillsInItem.Where(x => x.Bonus == 5).ToList();
        bool hasMaxSkill = skillBonus.Bonus == 5 && skillsInChangelist.Count > 0 || skillsInItem.Count > 0;


        result = hasMaxSkill ? ValidationEnum.LimitReached : result;
        string error = hasMaxSkill ? "Free personal skill bonus limit reached (The first +5 is factored into the point cost)." :
            anySkill ? $"You already have {skillBonus.Skill} on this item" : "";


        return new ValidationResult
        {
            Result = result,
            ErrorMessage = error
        };
    }

    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        SkillBonus skillBonus = new(incoming);

        bool isPersonalSkill = _personalSkills.Contains(skillBonus.Skill);
        if (isPersonalSkill)
        {
            // Logic for personal skills
        }

        return isPersonalSkill
            ? ValidatePersonalSkill(skillBonus, itemProperties, changelistProperties)
            : ValidateBeneficialSkill(skillBonus, itemProperties, changelistProperties);
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
            Result = result,
            ErrorMessage = error
        };
    }

    private class SkillBonus
    {
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

        public string Skill { get; }
        public int Bonus { get; }
        public static SkillBonus FromProperty(ItemProperty itemProperty) => new(itemProperty);
    }
}
