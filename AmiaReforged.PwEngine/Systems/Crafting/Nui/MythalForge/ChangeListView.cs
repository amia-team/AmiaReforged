using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public class ChangeListView
{
    public NuiBind<string> EntryLabels { get; set; } = new("entries");
    public NuiBind<string> EntryPowerCosts { get; set; } = new("entry_costs");
    public NuiBind<int> EntryCount { get; set; } = new("entry_count");

    public NuiElement View()
    {
        List<NuiListTemplateCell> cell = new()
        {
            new NuiListTemplateCell(new NuiLabel(EntryLabels)),
            new NuiListTemplateCell(new NuiLabel(EntryPowerCosts)),
            new NuiListTemplateCell(new NuiButton("X")
            {
                Id = "remove_change"
            })
        };

        return new NuiColumn
        {
            Children =
            {
                new NuiList(cell, EntryCount)
                {
                    RowHeight = 45f,
                    Width = 400f,
                    Height = 400f
                }
            }
        };
    }
}