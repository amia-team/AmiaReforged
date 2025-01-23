using System.Reflection;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;
using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(PropertyValidator))]
public class PropertyValidator
{
    readonly Dictionary<ItemPropertyType, IValidationRule> _validationRules;

    public PropertyValidator()
    {
        _validationRules = new Dictionary<ItemPropertyType, IValidationRule>();
        
        LoadValidationRules();
    }

    private void LoadValidationRules()
    {
        IEnumerable<Type> validationRuleTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<ValidationRuleFor>() != null && typeof(IValidationRule).IsAssignableFrom(t));

        foreach (Type type in validationRuleTypes)
        {
            ValidationRuleFor? attribute = type.GetCustomAttribute<ValidationRuleFor>();
            if (attribute == null) continue;
            
            IValidationRule? instance = (IValidationRule)Activator.CreateInstance(type);
            
            if (instance == null) continue;
            
            _validationRules[attribute.Property] = instance;
        }
    }
    
    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        if (!_validationRules.TryGetValue(incoming.ItemProperty.Property.PropertyType, out IValidationRule? operation))
        {
            return new ValidationResult
            {
                Result = PropertyValidationResult.Valid
            };
        }

        return operation.Validate(incoming, itemProperties, changelistProperties);
    }
}