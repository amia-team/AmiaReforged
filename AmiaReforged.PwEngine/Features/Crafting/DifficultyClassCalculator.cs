using System.Reflection;
using AmiaReforged.PwEngine.Features.Crafting.Models;
using AmiaReforged.PwEngine.Features.Crafting.Models.DifficultyClassCalculation;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Crafting;

/// <summary>
///     Injected by other services to calculate the skill check required for crafting an item.
/// </summary>
[ServiceBinding(typeof(DifficultyClassCalculator))]
public class DifficultyClassCalculator
{
    private readonly Dictionary<ItemPropertyType, IComputableDifficulty> _difficulties;

    public DifficultyClassCalculator()
    {
        _difficulties = new Dictionary<ItemPropertyType, IComputableDifficulty>();

        LoadDifficulties();
    }

    private void LoadDifficulties()
    {
        IEnumerable<Type> difficultyTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.GetCustomAttribute<ComputationRuleFor>() != null
                        && typeof(IComputableDifficulty).IsAssignableFrom(t));

        foreach (Type type in difficultyTypes)
        {
            ComputationRuleFor? attribute = type.GetCustomAttribute<ComputationRuleFor>();
            if (attribute == null) continue;

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
            IComputableDifficulty? instance = (IComputableDifficulty)Activator.CreateInstance(type);
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

            if (instance == null) continue;

            _difficulties[attribute.Property] = instance;
        }
    }

    public int ComputeDifficulty(CraftingProperty property)
    {
        if (!_difficulties.TryGetValue(property.ItemProperty.Property.PropertyType,
                out IComputableDifficulty? operation))
            // Generic difficulty class calculation
            return 10 + 6 * property.PowerCost;

        return operation.CalculateDifficultyClass(property);
    }
}
