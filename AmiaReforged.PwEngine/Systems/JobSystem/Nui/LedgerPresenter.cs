using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Nui;

[CreatedAtRuntime]
public sealed class LedgerPresenter(NwPlayer player, LedgerView view) : ScryPresenter<LedgerView>
{
    public override LedgerView View { get; } = view;
    public override NuiWindowToken Token() => _token;

    private NuiWindow? _window;
    private NuiWindowToken _token;

    private LedgerModel Model { get; } = new(player);
    
    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Ledger")
        {
            Geometry = new NuiRect(200, 200, 640, 480)
        };
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
            case LedgerConsts.LogsId:
                LoadCategory(ItemType.Log);
                break;
            case LedgerConsts.PlanksId:
                LoadCategory(ItemType.Plank);
                break;
            case LedgerConsts.OreId:
                LoadCategory(ItemType.Ore);
                break;
            case LedgerConsts.GemsId:
                LoadCategory(ItemType.Gem);
                break;
            case LedgerConsts.StoneId:
                LoadCategory(ItemType.Stone);
                break;
            case LedgerConsts.IngotsId:
                LoadCategory(ItemType.Ingot);
                break;
            case LedgerConsts.GrainId:
                LoadCategory(ItemType.Grain);
                break;
            case LedgerConsts.FlourId:
                LoadCategory(ItemType.Flour);
                break;
            case LedgerConsts.IngredientsId:
                LoadCategory(ItemType.FoodIngredient);
                break;
            case LedgerConsts.FoodId:
                LoadCategory(ItemType.Food);
                break;
            case LedgerConsts.DrinksId:
                LoadCategory(ItemType.Drink);
                break;
            case LedgerConsts.PotionIngredientsId:
                LoadCategory(ItemType.PotionIngredient);
                break;
            case LedgerConsts.PotionsId:
                LoadCategory(ItemType.Potion);
                break;
            case LedgerConsts.AcademiaId:
                LoadCategory(ItemType.Scholastic);
                break;
            case LedgerConsts.PeltsId:
                LoadCategory(ItemType.Pelt);
                break;
            case LedgerConsts.HidesId:
                LoadCategory(ItemType.Hide);
                break;
            case LedgerConsts.WeaponsId:
                LoadCategory(ItemType.Weapon);
                break;
            case LedgerConsts.ArmorId:
                LoadCategory(ItemType.Armor);
                break;
            case LedgerConsts.CraftsId:
                LoadCategory(ItemType.Crafts);
                return;
            default: return;
        }
    }

    private void LoadCategory(ItemType type)
    {
        NwCreature? character = player.LoginCreature;
        if(character is null) return;

        character.SpeakString($"Selected {type.ToString()}");
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
        _token.Close();
    }
}

[CreatedAtRuntime]
public sealed class LedgerModel(NwPlayer player)
{
}