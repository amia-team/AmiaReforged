using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.VisualBasic;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class MythalForgeController : NuiController<MythalForgeView>
{
    private const string TargetingModeMythalForge = "mythal_forge";
    private const string LvarTargetingMode = "targeting_mode";
    [Inject] private Lazy<CraftingPropertyData>? PropertyData { get; set; }
    [Inject] private Lazy<CraftingBudgetService>? BudgetService { get; set; }
    [Inject] private Lazy<ActiveCraftingData>? ActiveCraftingData { get; set; }

    private IReadOnlyList<CraftingCategory> _itemProperties;

    public override void Init()
    {
        if (PropertyData is null) return;
        if (BudgetService is null) return;
        if (ActiveCraftingData is null) return;

        _itemProperties = ActiveCraftingData.Value.SelectedCategoryFor(Token.Player);
        Token.SetBindValue(View.CategoryCount, _itemProperties.Count);
        
        IEnumerable<string> labels = _itemProperties.Select(p => p.Label).ToArray();

        // Token.SetBindValues(View.CategoryLabel, labels);

        List<NuiComboEntry> properties = new List<NuiComboEntry>();

        foreach (CraftingCategory category in _itemProperties)
        {
            properties.AddRange(category.Properties.Select((t, i) => t.ToComboEntry(i)));
        }
        
        CalculateBudget(ActiveCraftingData.Value.SelectedItemFor(Token.Player));
    }

    private void CalculateBudget(NwItem item)
    {
        if (BudgetService is null) return;

        int budget = BudgetService.Value.MythalBudgetFor(NWScript.GetBaseItemType(item));
        Token.SetBindValue(View.Budget, budget.ToString());

        int remaining = BudgetService.Value.RemainingBudgetFor(item);
        Token.SetBindValue(View.RemainingBudget, remaining.ToString());
    }
    

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
        }
    }

    protected override void OnClose()
    {
        ActiveCraftingData?.Value.ClearSelectedCategory(Token.Player);
    }
}