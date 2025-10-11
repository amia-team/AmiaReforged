using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class MonkPathPresenter(MonkPathView pathView, NwPlayer player) : ScryPresenter<MonkPathView>
{
    public override MonkPathView View { get; } = pathView;

    public override NuiWindowToken Token() => _token;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private static readonly NwClass? ObsoletePoeClass = NwClass.FromClassId(50);
    private const string WindowTitle = "Choose Your Path of Enlightenment";

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (Enum.TryParse(eventData.ElementId, out PathType path))
        {
            Token().SetBindValue(View.PathBind, path);
            UpdateConfirmPath();

            Token().SetBindValue(View.IsConfirmViewOpen, true);
        }
        else if (eventData.ElementId == View.ConfirmPathButton.Id)
        {
            ChoosePath();
        }
        else if (eventData.ElementId == View.BackButton.Id)
        {
            Token().SetBindValue(View.IsConfirmViewOpen, false);
        }
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 600, 640f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null) return;

        player.TryCreateNuiWindow(_window, out _token);

        Token().SetBindValue(View.IsConfirmViewOpen, false);
    }

    public override void Close()
    {
        _token.Close();
    }

    private void UpdateConfirmPath()
    {
        PathType? pathType = Token().GetBindValue(View.PathBind);
        if (pathType == null)
        {
            player.SendServerMessage("Could not find path!");
            return;
        }

        Token().SetBindValue(View.PathIcon, MonkPathMap.PathMap[pathType.Value].PathIcon);
        Token().SetBindValue(View.PathLabel, MonkPathMap.PathMap[pathType.Value].PathName);
        Token().SetBindValue(View.PathText, MonkPathMap.PathMap[pathType.Value].PathAbilities);
    }

    private void ChoosePath()
    {
        PathType? selectedPath = Token().GetBindValue(View.PathBind);

        if (selectedPath == null)
        {
            player.SendServerMessage("Could not find path!");
            return;
        }

        if (!MonkPathMap.PathToFeat.TryGetValue(selectedPath.Value, out NwFeat? selectedPathFeat) || selectedPathFeat == null)
        {
            player.SendServerMessage("Could not find path feat!");
            return;
        }

        NwCreature? monkCharacter = player.ControlledCreature;
        if (monkCharacter == null) return;

        NwFeat? poeBaseFeat = monkCharacter.Feats.FirstOrDefault(f => f.Id == MonkFeat.PoeBase);

        if (poeBaseFeat == null)
        {
            player.SendServerMessage("Could not find the feat required to open the selection window!");
            return;
        }

        if (monkCharacter.GetClassInfo(ObsoletePoeClass) != null)
        {
            player.SendServerMessage("Obsolete Path of Enlightenment prestige class found. " +
                                      "Rebuilding without that class allows you to select your path.");

            return;
        }

        NwFeat? existingPathFeat = monkCharacter.Feats.FirstOrDefault(f => MonkPathMap.PathToFeat.ContainsValue(f));
        if (existingPathFeat != null)
        {
            player.SendServerMessage($"{existingPathFeat.Name} removed");
            monkCharacter.RemoveFeat(existingPathFeat);
        }

        monkCharacter.AddFeat(selectedPathFeat, 12);
        player.SendServerMessage($"{selectedPathFeat.Name} added");

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        RaiseCloseEvent();

        // Allow people to switch the path feat on test
        if (environment != "live")
            return;

        monkCharacter.RemoveFeat(poeBaseFeat);
    }
}
