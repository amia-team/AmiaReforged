using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

[ServiceBinding(typeof(CraftingWindowManager))]
public class CraftingWindowManager
{
    private readonly Dictionary<NwPlayer, CraftingWindow?> _windows = new();

    private CraftingPropertyData PropertyData { get; }
    private CraftingBudgetService BudgetService { get; }

    public CraftingWindowManager(CraftingBudgetService budgetService, CraftingPropertyData propertyData)
    {
        BudgetService = budgetService;
        PropertyData = propertyData;
    }

    public void OpenWindow(NwPlayer player, NwItem selection)
    {
        if (_windows.TryGetValue(player, out CraftingWindow? value))
        {
            value?.CloseWindow();
            _windows.Remove(player);
        }

        CraftingWindow? window = new CraftingWindow(player, selection, PropertyData, BudgetService);
        _windows.Add(player, window);
        window.OpenWindow();

        player.OnClientLeave += CloseOnLeave;
    }

    private void CloseOnLeave(ModuleEvents.OnClientLeave obj)
    {
        if (!_windows.TryGetValue(obj.Player, out CraftingWindow? _)) return;
        
        this.CloseWindow(obj.Player);
    }

    public void CloseWindow(NwPlayer player)
    {
        if (!_windows.TryGetValue(player, out CraftingWindow? window)) return;
        if (window == null) return;

        window.CloseWindow();
        _windows.Remove(player);
    }
}