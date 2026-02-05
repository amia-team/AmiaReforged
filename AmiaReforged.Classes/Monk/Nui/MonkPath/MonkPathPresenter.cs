using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Types;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Nui.MonkPath;

public sealed class MonkPathPresenter(MonkPathView pathView, NwPlayer player) : ScryPresenter<MonkPathView>
{
    public override MonkPathView View { get; } = pathView;
    private MonkPathModel Model { get; } = new();

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
        int token = eventData.Token.Token;

        if (Enum.TryParse(eventData.ElementId, out PathType path))
        {
            Token().SetBindValue(View.PathBind, path);
            UpdateConfirmPath(path);

            Token().SetBindValue(View.IsConfirmViewOpen, true);

            foreach (MonkPathModel.MonkPathData pathData in Model.Paths)
            {
                NuiBind<bool> glowBind = new($"glow_{pathData.Type}");

                glowBind.SetBindValue(player, token, pathData.Type == path);
            }
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

    private void UpdateConfirmPath(PathType path)
    {
        var pathData = Model.Get(path);
        if (pathData == null)
        {
            player.SendServerMessage("Could not find path data!");
            return;
        }

        Token().SetBindValue(View.PathIcon, pathData.Icon);
        Token().SetBindValue(View.PathLabel, pathData.Name);
        Token().SetBindValue(View.PathText, pathData.Abilities);
    }

    private void ChoosePath()
    {
        PathType? selectedPath = Token().GetBindValue(View.PathBind);

        if (selectedPath == null)
        {
            player.SendServerMessage("Could not find path!");
            return;
        }

        var pathData = Model.Get(selectedPath.Value);
        if (pathData == null)
        {
            player.SendServerMessage("Could not find path data!");
            return;
        }

        NwFeat? selectedPathFeat = NwFeat.FromFeatId(pathData.FeatId);
        if (selectedPathFeat == null)
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

        foreach (var path in Model.Paths)
        {
            NwFeat? feat = NwFeat.FromFeatId(path.FeatId);
            if (feat == null || !monkCharacter.KnowsFeat(feat)) continue;

            player.SendServerMessage($"{feat.Name} removed");
            monkCharacter.RemoveFeat(feat);
        }

        monkCharacter.AddFeat(selectedPathFeat, 12);
        player.SendServerMessage($"{selectedPathFeat.Name} added");

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        RaiseCloseEvent();

        // Allow people to switch the path feat on test
        if (environment != "live")
            return;

        monkCharacter.RemoveFeat(poeBaseFeat, true);
    }
}
