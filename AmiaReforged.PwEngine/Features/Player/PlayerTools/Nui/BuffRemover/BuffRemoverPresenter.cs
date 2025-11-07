using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.BuffRemover;

public class BuffRemoverPresenter : ScryPresenter<BuffRemoverView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public BuffRemoverPresenter(BuffRemoverView view, NwPlayer player)
    {
        View = view;
        _player = player;
        Model = new BuffRemoverModel(player);

        NwCreature? character = player.LoginCreature;

        if (character == null)
        {
            player.SendServerMessage(message: "Character could not be found. Please relog.", ColorConstants.Orange);
            return;
        }

        character.OnEffectApply += OnEffectApply;
        character.OnEffectRemove += OnEffectRemove;
    }

    private BuffRemoverModel Model { get; }

    public override BuffRemoverView View { get; }

    private void OnEffectRemove(OnEffectRemove obj)
    {
        if (_window == null) return;
        UpdateView();
    }

    private void OnEffectApply(OnEffectApply obj)
    {
        if (_window == null) return;
        UpdateView();
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(400, 400, 670f, 520f),
            Resizable = false
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
        if (click.ElementId == View.RemoveAllButton.Id)
        {
            _player.SendServerMessage(message: "Removing all effects.", ColorConstants.Orange);
            Model.RemoveAllEffects();
            UpdateView();

            return;
        }

        if (click.ElementId == "remove_effect")
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
        if (_window == null) InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        UpdateView();
    }

    public override void Close()
    {
        NwCreature? character = _player.LoginCreature;
        if (character == null)
        {
            _player.SendServerMessage(message: "Character could not be found. Please relog.", ColorConstants.Orange);
            return;
        }

        character.OnEffectApply -= OnEffectApply;
        character.OnEffectRemove -= OnEffectRemove;
    }
}
