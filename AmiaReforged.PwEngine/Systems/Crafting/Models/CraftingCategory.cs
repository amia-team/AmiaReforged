using AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge.SubViews.ChangeList;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Models;

public class CraftingCategory
{
    public string CategoryId { get; set; }
    public CraftingCategory(string categoryId)
    {
        CategoryId = categoryId;
        ComboSelection = new NuiBind<int>(categoryId);
        ShowGroup = new NuiBind<bool>(categoryId + "_show");
    }

    public NuiBind<bool> ShowGroup { get; set; }
    
    public int BaseDifficulty { get; set; }

    public NuiBind<int> ComboSelection { get; set; }
    public required string Label { get; set; }
    public required IReadOnlyList<CraftingProperty> Properties { get; init; }
    
    /// <summary>
    /// Provides a functional interface to validate a property before it is applied to an item.
    /// </summary>
    public Func<CraftingProperty, NwItem, List<ChangeListModel.ChangelistEntry>, PropertyValidationResult>? PerformValidation { get; set; }
    
}