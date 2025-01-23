using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

[ValidationRuleFor(Property = ItemPropertyType.SkillBonus)]
public class SkillBonusValidator : IValidationRule
{
    private const int MaxPersonalSkills = 2;
    
    private List<string> PersonalSkills = new List<string>
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
    
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties, List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        ValidationEnum result = ValidationEnum.Valid;
        string error = string.Empty;
        
        SkillBonus skillBonus = new(incoming);
        LogManager.GetCurrentClassLogger().Info($"SkillBonus: {skillBonus.Skill} {skillBonus.Bonus}");
        
        return new ValidationResult
        {
            Enum = result,
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
            
            Skill = model.SubTypeName.Replace("SkillBonus: ", "");
            Bonus = int.Parse(model.PropertyBonus);
        }
        
        public string Skill { get; set; }
        public int Bonus { get; set; }
    }
}