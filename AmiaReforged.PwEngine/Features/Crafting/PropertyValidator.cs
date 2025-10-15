using System.Reflection;
using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Models.PropertyValidationRules;
using AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Crafting;

[ServiceBinding(typeof(PropertyValidator))]
public class PropertyValidator
{
    private readonly Dictionary<ItemPropertyType, IValidationRule> _validationRules;

    public PropertyValidator()
    {
        _validationRules = new Dictionary<ItemPropertyType, IValidationRule>();

        LoadValidationRules();
    }

    private void LoadValidationRules()
    {
        IEnumerable<Type> validationRuleTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<ValidationRuleFor>() != null
                        && typeof(IValidationRule).IsAssignableFrom(t));

        foreach (Type type in validationRuleTypes)
        {
            ValidationRuleFor? attribute = type.GetCustomAttribute<ValidationRuleFor>();
            if (attribute == null) continue;

            // Disabled with a pragma because this is absolutely necessary.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            IValidationRule? instance = (IValidationRule)Activator.CreateInstance(type);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (instance == null) continue;

            _validationRules[attribute.Property] = instance;
        }
    }

    public ValidationResult Validate(CraftingProperty incoming, IEnumerable<ItemProperty> itemProperties,
        List<ChangeListModel.ChangelistEntry> changelistProperties)
    {
        if (!_validationRules.TryGetValue(incoming.ItemProperty.Property.PropertyType, out IValidationRule? operation))
            return new ValidationResult
            {
                Result = ValidationEnum.Valid
            };

        return operation.Validate(incoming, itemProperties, changelistProperties);
    }
}
