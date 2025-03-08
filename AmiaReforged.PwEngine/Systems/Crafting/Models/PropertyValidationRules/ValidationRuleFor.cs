using Anvil.API;
using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models.PropertyValidationRules;

/// <summary>
///     Attribute to define a validation rule for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
public class ValidationRuleFor : Attribute
{
    public ItemPropertyType Property { get; init; }
}