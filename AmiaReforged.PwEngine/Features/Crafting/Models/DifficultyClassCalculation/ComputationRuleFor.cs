using Anvil.API;
using JetBrains.Annotations;

namespace AmiaReforged.PwEngine.Features.Crafting.Models.DifficultyClassCalculation;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[MeansImplicitUse(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature, ImplicitUseTargetFlags.Itself)]
public class ComputationRuleFor : Attribute
{
    public ItemPropertyType Property { get; init; }
}
