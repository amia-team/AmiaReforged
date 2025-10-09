using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class ConfirmPathPresenter : ScryPresenter<ConfirmPathView>
{
    private readonly MonkPathPresenter _parent;
    private readonly NwPlayer _player;
    private static readonly NwClass? ObsoletePoeClass = NwClass.FromClassId(50);
    private NuiWindow? _window;
    private NuiWindowToken _token;

    public ConfirmPathPresenter(MonkPathPresenter parent, NwPlayer player, ConfirmPathView toolView)
    {
        View = toolView;
        _parent = parent;
        _player = player;

        parent.ViewUpdated += UpdateConfirmPath;
        parent.PathSelectionClosing += HandleClose;
    }

    public override ConfirmPathView View { get; }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }

    private void HandleClose(MonkPathPresenter sender)
    {
        Close();
    }

    private void UpdateConfirmPath(MonkPathPresenter sender, MonkPathView senderView)
    {
        PathType? pathType = sender.Token().GetBindValue(senderView.PathBind);
        if (pathType == null)
        {
            _player.SendServerMessage("Could not find path!");
            return;
        }

        Token().SetBindValue(View.PathIcon, MonkPathMap.PathMap[pathType.Value].PathIcon);
        Token().SetBindValue(View.PathLabel, MonkPathMap.PathMap[pathType.Value].PathName);
        Token().SetBindValue(View.PathText, MonkPathMap.PathMap[pathType.Value].PathAbilities);
        Token().SetBindValue(View.SelectedPath, pathType.Value);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId != View.ConfirmPathButton.Id)
        {
            _player.SendServerMessage("Something clicked!");
            return;
        }


        _player.SendServerMessage("Confirm button clicked!");

        PathType? selectedPath = Token().GetBindValue(View.SelectedPath);

        if (selectedPath == null)
        {
            _player.SendServerMessage("Could not find path!");
            return;
        }

        if (!MonkPathMap.PathToFeat.TryGetValue(selectedPath.Value, out NwFeat? selectedPathFeat) || selectedPathFeat == null)
        {
            _player.SendServerMessage("Could not find path feat!");
            return;
        }

        NwCreature? monkCharacter = _player.ControlledCreature;
        if (monkCharacter == null) return;

        NwFeat? poeBaseFeat = monkCharacter.Feats.FirstOrDefault(f => f.Id == MonkFeat.PoeBase);

        if (poeBaseFeat == null)
        {
            _player.SendServerMessage("Could not find the feat required to open the selection window!");
            return;
        }

        if (monkCharacter.GetClassInfo(ObsoletePoeClass) != null)
        {
            _player.SendServerMessage("Obsolete Path of Enlightenment prestige class found. " +
                                      "Rebuilding without that class allows you to select your path.");

            return;
        }

        NwFeat? existingPathFeat = monkCharacter.Feats.FirstOrDefault(f => MonkPathMap.PathToFeat.ContainsValue(f));
        if (existingPathFeat != null)
        {
            _player.SendServerMessage($"{existingPathFeat.Name} removed");
            monkCharacter.RemoveFeat(existingPathFeat);
        }

        monkCharacter.AddFeat(selectedPathFeat, 12);
        _player.SendServerMessage($"{selectedPathFeat.Name} added");

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        RaiseCloseEvent();
        _parent.RaiseCloseEvent();

        // Allow people to switch the path feat on test
        if (environment != "live")
            return;

        monkCharacter.RemoveFeat(poeBaseFeat);
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "")
        {
            Geometry = new NuiRect(400f, 400f, 600, 640f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null) return;

        _player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
        _token.Close();
    }

}
