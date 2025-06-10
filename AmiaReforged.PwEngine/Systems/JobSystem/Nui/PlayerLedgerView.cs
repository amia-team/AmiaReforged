using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui;

public sealed class PlayerLedgerView : ScryView<PlayerLedgerPresenter>
{
    public override PlayerLedgerPresenter Presenter { get; protected set; }

    // Never null...
    public NuiButton LogsButton = null!;
    public NuiButton PlanksButton = null!;
    public NuiButton OreButton = null!;
    public NuiButton GemsButton = null!;
    public NuiButton StoneButton = null!;
    public NuiButton IngotsButton = null!;
    public NuiButton GrainButton = null!;
    public NuiButton FlourButton = null!;
    public NuiButton IngredientsButton = null!;
    public NuiButton FoodButton = null!;
    public NuiButton DrinksButton = null!;
    public NuiButton PotionIngredientsButton = null!;
    public NuiButton PotionsButton = null!;
    public NuiButton AcademiaButton = null!;
    public NuiButton PeltsButton = null!;
    public NuiButton HidesButton = null!;
    public NuiButton WeaponsButton = null!;
    public NuiButton ArmorButton = null!;
    public NuiButton CraftsButton = null!;

    public ItemType SelectedCategory { get; set; } = ItemType.Log;

    public readonly NuiBind<string> MaterialNames = new("item_names");
    public readonly NuiBind<string> AverageQualities = new("average_qualities");
    public readonly NuiBind<string> Amounts = new("amounts");
    public readonly NuiBind<int> CellCount = new("cell_count");

    public PlayerLedgerView(NwPlayer player)
    {
        Presenter = new(player, this);
    }

    public override NuiLayout RootLayout()
    {
        List<NuiListTemplateCell> cells =
        [
            new(new NuiLabel(MaterialNames))
            {
            },

            new(new NuiLabel(Amounts))
            {
            },
            new(new NuiLabel(AverageQualities))
            {
            }

        ];
        NuiColumn nuiElement = new()
        {
            Children =
            {
                new NuiButton("Logs")
                {
                    Id = LedgerBindingConsts.LogsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Uncut logs, useful for heavy constructions and processing to planks"
                }.Assign(out LogsButton),
                new NuiButton("Planks")
                {
                    Id = LedgerBindingConsts.PlanksId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Planks used for construction"
                }.Assign(out PlanksButton),
                new NuiButton("Ore")
                {
                    Id = LedgerBindingConsts.OreId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Unsmelted ores"
                }.Assign(out OreButton),
                new NuiButton("Gems")
                {
                    Id = LedgerBindingConsts.GemsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Rough and processed gems"
                }.Assign(out GemsButton),
                new NuiButton("Stone")
                {
                    Id = LedgerBindingConsts.StoneId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Stone used in building"
                }.Assign(out GemsButton),
                new NuiButton("Ingots")
                {
                    Id = LedgerBindingConsts.IngotsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Ore that has been processed into bars"
                }.Assign(out IngotsButton),
                new NuiButton("Pelts")
                {
                    Id = LedgerBindingConsts.PeltsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Unprocessed job system pelts"
                }.Assign(out PeltsButton),
                new NuiButton("Hides")
                {
                    Id = LedgerBindingConsts.HidesId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Processed job system pelts"
                }.Assign(out HidesButton),
                new NuiButton("Grain")
                {
                    Id = LedgerBindingConsts.GrainId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Food that can be stored for long periods of time to be processed later"
                }.Assign(out GrainButton),
                new NuiButton("Flour")
                {
                    Id = LedgerBindingConsts.FlourId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Byproduct of job system grains for making bread"
                }.Assign(out FlourButton),
                new NuiButton("Ingredients")
                {
                    Id = LedgerBindingConsts.IngredientsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Job system cooking ingredients"
                }.Assign(out IngredientsButton),
                new NuiButton("Food")
                {
                    Id = LedgerBindingConsts.FoodId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Job system meals"
                }.Assign(out FoodButton),
                new NuiButton("Drinks")
                {
                    Id = LedgerBindingConsts.DrinksId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Alcohol, juice, other job system drinks"
                }.Assign(out DrinksButton),
                new NuiButton("Alchemy Ingredients")
                {
                    Id = LedgerBindingConsts.PotionIngredientsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Alchemical reagents"
                }.Assign(out PotionIngredientsButton),
                new NuiButton("Potions")
                {
                    Id = LedgerBindingConsts.PotionsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Job system potions"
                }.Assign(out PotionsButton),
                new NuiButton("Academic")
                {
                    Id = LedgerBindingConsts.AcademiaId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Scholarly items, artificer components, etc."
                }.Assign(out AcademiaButton),
                new NuiButton("Weapons")
                {
                    Id = LedgerBindingConsts.WeaponsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Job System Weapons"
                }.Assign(out WeaponsButton),
                new NuiButton("Armor")
                {
                    Id = LedgerBindingConsts.ArmorId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Job System Armor"
                }.Assign(out ArmorButton),
                new NuiButton("Crafts")
                {
                    Id = LedgerBindingConsts.CraftsId,
                    Width = 100f,
                    Height = 60f,
                    Tooltip = "Jewelry, Paintings, Etc"
                }.Assign(out CraftsButton),
            }
        };
        NuiColumn root = new()
        {
            Children =
            {
                new NuiRow()
                {
                    Children =
                    {
                        nuiElement,
                        new NuiList(cells, CellCount)
                    }
                }
            }
        };

        return root;
    }
}