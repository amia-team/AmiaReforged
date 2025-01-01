using AmiaReforged.PwEngine.Systems.Crafting.Models;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Crafting;

[ServiceBinding(typeof(ActiveCraftingData))]
public class ActiveCraftingData
{
    private Dictionary<string, IReadOnlyList<CraftingCategory>> SelectedCategories { get; } = new();
    private Dictionary<string, NwItem> SelectedItems { get; } = new();


    public IReadOnlyList<CraftingCategory> SelectedCategoryFor(NwPlayer player) =>
        SelectedCategories[player.PlayerName];

    public void SetSelectedCategory(NwPlayer player, IReadOnlyList<CraftingCategory> category) =>
        SelectedCategories[player.PlayerName] = category;

    public void ClearSelectedCategory(NwPlayer player) =>
        SelectedCategories.Remove(player.PlayerName);

    public NwItem SelectedItemFor(NwPlayer player) => SelectedItems[player.PlayerName];

    public void SetSelectedItem(NwPlayer player, NwItem item) =>
        SelectedItems[player.PlayerName] = item;
}