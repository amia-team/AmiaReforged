using AmiaReforged.Core.UserInterface;

namespace AmiaReforged.Settlements.UI.ResourceManagement;

public class StockpileView : WindowView<StockpileView>
{
    public override string Id => "settlements.stockpileview";
    public sealed override string Title => "Stockpile Overview";
    public override bool ListInPlayerTools => false;

    public override IWindowController? CreateDefaultController(NwPlayer player)
    {
        return CreateController<StockpileController>(player);
    }

    public override NuiWindow? WindowTemplate { get; }

    public readonly NuiBind<string> Search = new("search_val");
    public readonly NuiBind<int> Resources = new("loaded_resources");
    public readonly NuiBind<string> ResourceNames = new("res_names");
    public readonly NuiBind<string> ResourceCountStockpile = new("res_count");
    public readonly NuiBind<string> ResourceCountPlayer = new("res_count_player");
    public readonly NuiBind<string> TransferAmount = new("transfer_amount");

    public readonly NuiButtonImage SearchButton;
    public readonly NuiButton LogsButton;
    public readonly NuiButton PlanksButton;
    public readonly NuiButton OreButton;
    public readonly NuiButton IngotsButton;
    public readonly NuiButton GrainButton;
    public readonly NuiButton MeatButton;
    public readonly NuiButton FishButton;
    public readonly NuiButton VegetableButton;
    public readonly NuiButton FruitButton;
    public readonly NuiButton HerbButton;
    public readonly NuiButton DrinksButton;

    public StockpileView()
    {
        List<NuiListTemplateCell> rowTemplate = new()
        {
            new NuiListTemplateCell(new NuiLabel(ResourceNames)
            {
                VerticalAlign = NuiVAlign.Middle,
            }),
            new NuiListTemplateCell(new NuiLabel(ResourceCountStockpile)
            {
                VerticalAlign = NuiVAlign.Middle,
            }),
            new NuiListTemplateCell(new NuiLabel(ResourceCountPlayer)
            {
                VerticalAlign = NuiVAlign.Middle,
            }),
            new NuiListTemplateCell(new NuiTextEdit("0", TransferAmount, 3, false)),
            new NuiListTemplateCell(new NuiButton("+"))
        };

        NuiRow root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiColumn
                {
                    Children = new List<NuiElement>
                    {
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Logs").Assign(out LogsButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Planks").Assign(out PlanksButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Ore").Assign(out OreButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Ingots").Assign(out IngotsButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Grain").Assign(out GrainButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Meat").Assign(out MeatButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Fish").Assign(out FishButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Vegetables").Assign(out VegetableButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Fruit").Assign(out FruitButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Herbs").Assign(out HerbButton),
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiButton("Drinks").Assign(out DrinksButton),
                            }
                        } 
                    }
                },
                new NuiColumn
                {
                    Children = new List<NuiElement>
                    {
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiTextEdit("Search Resources...", Search, 255, false),
                                new NuiButtonImage("isk_search")
                                {
                                    Id = "btn_search",
                                    Aspect = 1f,
                                }.Assign(out SearchButton)
                            }
                        },
                        new NuiRow
                        {
                            Children = new List<NuiElement>
                            {
                                new NuiList(rowTemplate, Resources)
                                {
                                    RowHeight = 35f
                                }
                            }
                        }
                    }
                }
            }
        };

        WindowTemplate = new NuiWindow(root, Title)
        {
            Geometry = new NuiRect(0f, 100f, 400f, 600f),
        };
    }
}