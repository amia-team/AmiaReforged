using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public sealed class MythalForgeView : NuiView<MythalForgeView>
{
    public override string Id => "crafting.mythal_forge";
    public override string Title => "Mythal Forge";
    public override Anvil.API.NuiWindow? WindowTemplate { get; }

    public readonly NuiButtonImage SelectItemButton;
    public readonly NuiBind<string> PropertyCategories = new NuiBind<string>("labels");
    public readonly NuiBind<int> PropertyCount = new NuiBind<int>("count");

    public readonly NuiBind<string> Budget = new NuiBind<string>("budget");
    public readonly NuiBind<string> RemainingBudget = new NuiBind<string>("spent");

    public override INuiController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<MythalForgeController>(player);
    }

    public MythalForgeView()
    {
        List<NuiListTemplateCell> rowTemplate = new()
        {
            new NuiListTemplateCell(new NuiButtonSelect(PropertyCategories, selected: false)
            {
                Id = "btn_select",
                Aspect = 1f,
            })
        };
        List<NuiListTemplateCell> budgetRowTemplate = new()
        {
            new NuiListTemplateCell(new NuiRow()
            {
                Children =
                {
                    new NuiLabel("Budget:"),
                    new NuiLabel(Budget)
                }
            }),
            new NuiListTemplateCell(new NuiRow
            {
                Children =
                {
                    new NuiLabel("Remaining:"),
                    new NuiLabel(RemainingBudget)
                }
            }),
        };

        NuiRow root = new()
        {
            Children =
            {
                new NuiColumn()
                {
                    Children =
                    {
                        new NuiLabel("Select an item to craft:"),
                        new NuiButtonImage("ir_sell02")
                        {
                            Id = "btn_selectitem",
                            Aspect = 1f,
                            Tooltip = "Select Item",
                        }.Assign(out SelectItemButton)
                    },
                },
                new NuiRow
                {
                    Children = new List<NuiElement>()
                    {
                        new NuiList(budgetRowTemplate, 1)
                        {
                            RowHeight = 35f,
                            Width = 400,
                            Height = 100
                        }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>()
                    {
                        new NuiList(rowTemplate, PropertyCount)
                        {
                            RowHeight = 35f,
                            Width = 400,
                            Height = 400
                        }
                    }
                }
            }
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(100f, 100f, 400f, 600f),
        };
    }
}