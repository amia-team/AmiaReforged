using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.BuffRemover;

public class BuffRemoverPresenter : ScryPresenter<BuffRemoverView>
{
    private BuffRemoverModel Model { get; }
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;

    public BuffRemoverPresenter(BuffRemoverView view, NwPlayer player)
    {
        View = view;
        _player = player;
        Model = new BuffRemoverModel(player);
        
        NwCreature? character = player.LoginCreature;
        
        if (character == null)
        {
            player.SendServerMessage("Character could not be found. Please relog.", ColorConstants.Orange);
            return;
        }
        
        character.OnEffectApply += OnEffectApply;
        character.OnEffectRemove += OnEffectRemove;
    }

    private void OnEffectRemove(OnEffectRemove obj)
    {
        UpdateView();
    }

    private void OnEffectApply(OnEffectApply obj)
    {
        UpdateView();
    }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(400, 400, 250f, 500f),
            // Resizable = false TODO: Uncomment when UX is done
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

    private void HandleButtonClick(ModuleEvents.OnNuiEvent click)
    {
        if(click.ElementId == View.RemoveAllButton.Id)
        {
            _player.SendServerMessage("Removing all effects.", ColorConstants.Orange);
            Model.RemoveAllEffects();
            UpdateView();

            return;
        }
        
        if(click.ElementId == "remove_effect")
        {
            Model.RemoveEffectAt(click.ArrayIndex);
            UpdateView();
        }
    }

    public override void UpdateView()
    {
        Model.UpdateEffectList();
        
        Token().SetBindValues(View.EffectLabels, Model.Labels);
        Token().SetBindValue(View.BuffCount, Model.Labels.Count);
    }

    public override void Create()
    {
        if (_window == null)
        {
            InitBefore();
        }

        if (_window == null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
        NwCreature? character = _player.LoginCreature;
        if (character == null)
        {
            _player.SendServerMessage("Character could not be found. Please relog.", ColorConstants.Orange);
            return;
        }
        
        character.OnEffectApply -= OnEffectApply;
        character.OnEffectRemove -= OnEffectRemove;
    }

    public override BuffRemoverView View { get; }
}