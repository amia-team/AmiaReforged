using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.Crafting.Models;
using AmiaReforged.PwEngine.Systems.WindowingSystem;
using Anvil.API;
using Anvil.Services;
using Microsoft.Extensions.Logging;
using NLog;

namespace AmiaReforged.PwEngine.Systems.Crafting.Nui.MythalForge;

public sealed class MythalForgeView : NuiView<MythalForgeView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public override string Id => "crafting.mythal_forge";
    public override string Title => "Mythal Forge";
    public override NuiWindow? WindowTemplate { get; }

    public readonly NuiBind<string> PropertyCategories = new NuiBind<string>("labels");
    public readonly NuiBind<int> PropertyCount = new NuiBind<int>("count");
    
    public readonly NuiBind<int> CategoryCount = new NuiBind<int>("count");

    public readonly NuiBind<string> Budget = new NuiBind<string>("budget");
    public readonly NuiBind<string> RemainingBudget = new NuiBind<string>("spent");
    
    private INuiController? _controller;
    
    public override INuiController? CreateDefaultController(NwPlayer player)
    {
        _controller = CreateController<MythalForgeController>(player);
        return _controller;
    }

    public MythalForgeView()
    {
        Log.Info("Mythal Forge view initialized.");

        List<NuiListTemplateCell> categoryTemplate = new()
        {
            new NuiListTemplateCell(new NuiLabel(PropertyCategories)),
            new NuiListTemplateCell(new NuiCombo())
        };
        
        List<NuiListTemplateCell> budgetRowTemplate = new()
        {
            new NuiListTemplateCell(new NuiRow
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
                        new NuiList(categoryTemplate, CategoryCount)
                    }
                },
                new NuiColumn
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
                }
            }
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(100f, 100f, 400f, 600f),
        };
    }
}