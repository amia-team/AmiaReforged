using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Nui.ViewModels;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui;

[CreatedAtRuntime]
public sealed class PlayerLedgerPresenter(NwPlayer player, PlayerLedgerView view) : ScryPresenter<PlayerLedgerView>
{
    public override PlayerLedgerView View { get; } = view;
    public override NuiWindowToken Token() => _token;

    private NuiWindow? _window;
    private NuiWindowToken _token;

    private PlayerLedgerModel Model { get; } = new(player);

    public override void InitBefore()
    {
        _window = new(View.RootLayout(), "Your Job System Ledger")
        {
            Geometry = new NuiRect(200, 200, 640, 480)
        };

        if (player.LoginCreature == null) return;

        player.LoginCreature.OnAcquireItem += OnAcquireItem;
    }

    private void OnAcquireItem(ModuleEvents.OnAcquireItem obj)
    {
        if (obj.AcquiredBy != player.LoginCreature) return;

        Model.RefreshLedger();
        UpdateLedgerView();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(obj);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent evnt)
    {
        switch (evnt.ElementId)
        {
            case LedgerBindingConsts.LogsId:
                LoadCategory(ItemType.Log);
                break;
            case LedgerBindingConsts.PlanksId:
                LoadCategory(ItemType.Plank);
                break;
            case LedgerBindingConsts.OreId:
                LoadCategory(ItemType.Ore);
                break;
            case LedgerBindingConsts.GemsId:
                LoadCategory(ItemType.Gem);
                break;
            case LedgerBindingConsts.StoneId:
                LoadCategory(ItemType.Stone);
                break;
            case LedgerBindingConsts.IngotsId:
                LoadCategory(ItemType.Ingot);
                break;
            case LedgerBindingConsts.GrainId:
                LoadCategory(ItemType.Grain);
                break;
            case LedgerBindingConsts.FlourId:
                LoadCategory(ItemType.Flour);
                break;
            case LedgerBindingConsts.IngredientsId:
                LoadCategory(ItemType.FoodIngredient);
                break;
            case LedgerBindingConsts.FoodId:
                LoadCategory(ItemType.Food);
                break;
            case LedgerBindingConsts.DrinksId:
                LoadCategory(ItemType.Drink);
                break;
            case LedgerBindingConsts.PotionIngredientsId:
                LoadCategory(ItemType.PotionIngredient);
                break;
            case LedgerBindingConsts.PotionsId:
                LoadCategory(ItemType.Potion);
                break;
            case LedgerBindingConsts.AcademiaId:
                LoadCategory(ItemType.Scholastic);
                break;
            case LedgerBindingConsts.PeltsId:
                LoadCategory(ItemType.Pelt);
                break;
            case LedgerBindingConsts.HidesId:
                LoadCategory(ItemType.Hide);
                break;
            case LedgerBindingConsts.WeaponsId:
                LoadCategory(ItemType.Weapon);
                break;
            case LedgerBindingConsts.ArmorId:
                LoadCategory(ItemType.Armor);
                break;
            case LedgerBindingConsts.CraftsId:
                LoadCategory(ItemType.Crafts);
                return;
            default: return;
        }
    }

    private void LoadCategory(ItemType type)
    {
        NwCreature? character = player.LoginCreature;
        if (character is null) return;

        View.SelectedCategory = type;

        UpdateLedgerView();
    }

    private void UpdateLedgerView()
    {
        LedgerCategoryViewModel? viewModel = Model.ViewModelFor(View.SelectedCategory);

        if (viewModel is null)
        {
            ClearScreen();
            return;
        }

        // The first row of these are headers: Material | Amount | Average Quality
        List<string> materialNames =
            viewModel.CategoryEntries.Keys.Select(k => k.ToString()).Prepend("Material").ToList();
        List<string> amounts = viewModel.CategoryEntries.Values.Select(v => v.Count).Prepend("Amount").ToList();
        List<string> averageQualities = viewModel.CategoryEntries.Values.Select(v => v.AverageQuality)
            .Prepend("Overall Quality").ToList();

        Token().SetBindValues(View.MaterialNames, materialNames);
        Token().SetBindValues(View.Amounts, amounts);
        Token().SetBindValues(View.AverageQualities, averageQualities);

        Token().SetBindValue(View.CellCount, materialNames.Count);
    }

    private void ClearScreen()
    {
        Token().SetBindValue(View.CellCount, 0);

        Token().SetBindValues(View.MaterialNames, new List<string>());
        Token().SetBindValues(View.Amounts, new List<string>());
        Token().SetBindValues(View.AverageQualities, new List<string>());
    }

    public override void Create()
    {
        if (_window == null)
        {
            InitBefore();
        }

        if (_window == null)
        {
            player.FloatingTextString("Failed to create ledger window. Report this bug in the Discord.");
            return;
        }

        player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
        if (player.LoginCreature != null) player.LoginCreature.OnAcquireItem -= OnAcquireItem;
        _token.Close();
    }
}