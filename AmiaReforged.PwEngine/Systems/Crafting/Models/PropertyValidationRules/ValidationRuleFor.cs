using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

/// <summary>
/// Attribute to define a validation rule for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ValidationRuleFor : Attribute
{
    public ItemPropertyType Property { get; init; }
}