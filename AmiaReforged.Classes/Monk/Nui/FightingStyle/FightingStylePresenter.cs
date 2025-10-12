using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using static AmiaReforged.Classes.Monk.Nui.FightingStyle.FightingStyleNuiElements;

namespace AmiaReforged.Classes.Monk.Nui.FightingStyle;

public sealed class FightingStylePresenter(FightingStyleView view, NwPlayer player) : ScryPresenter<FightingStyleView>
{
    public override FightingStyleView View { get; } = view;
    public override NuiWindowToken Token() => _token;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private const string WindowTitle = "Choose Your Fighting Style";

    private static readonly Dictionary<string, NwFeat[]> FightingStyleFeats = new()
    {
        { KnockdownStyleName, [NwFeat.FromFeatType(Feat.Knockdown)!, NwFeat.FromFeatType(Feat.ImprovedKnockdown)!] },
        { DisarmStyleName, [NwFeat.FromFeatType(Feat.Disarm)!, NwFeat.FromFeatType(Feat.ImprovedDisarm)!] },
        { RangedStyleName, [NwFeat.FromFeatType(Feat.CalledShot)!, NwFeat.FromFeatType(Feat.PointBlankShot)!] }
    };

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.KnockdownStyleButton.Id)
            ChooseStyle(KnockdownStyleName);
        else if (eventData.ElementId == View.DisarmStyleButton.Id)
            ChooseStyle(DisarmStyleName);
        else if (eventData.ElementId == View.RangedStyleButton.Id)
            ChooseStyle(RangedStyleName);
    }

    private void ChooseStyle(string styleName)
    {
        if (!FightingStyleFeats.TryGetValue(styleName, out NwFeat[]? featsToAdd))
        {
            player.SendServerMessage("Fighting style feats not found.");

            return;
        }

        NwCreature? monk = player.ControlledCreature;
        if (monk == null) return;

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        if (monkLevel < 6) return;

        if (featsToAdd.Any(f => monk.KnowsFeat(f)))
        {
            player.SendServerMessage($"You already know one feat of the {styleName} fighting style. Select another style.");

            return;
        }

        if (HasMonkFightingStyle(monk)) return;

        foreach (NwFeat feat in featsToAdd)
        {
            monk.AddFeat(feat, 6);
        }

        player.FloatingTextString($"Added feats {featsToAdd[0].Name} and {featsToAdd[1].Name}", false);
    }

    private bool HasMonkFightingStyle(NwCreature monk)
    {
        NwFeat[] level6Feats = monk.LevelInfo[5].Feats.ToArray();

        if (FightingStyleFeats[KnockdownStyleName].All(styleFeat => level6Feats.Any(charFeat => charFeat.Id == styleFeat.Id)))
        {
            player.SendServerMessage($"You have already selected {KnockdownStyleName} for your Fighting Style.");
            return true;
        }
        if (FightingStyleFeats[DisarmStyleName].All(styleFeat => level6Feats.Any(charFeat => charFeat.Id == styleFeat.Id)))
        {
            player.SendServerMessage($"You have already selected {DisarmStyleName} for your Fighting Style.");
            return true;
        }
        if (FightingStyleFeats[RangedStyleName].All(styleFeat => level6Feats.Any(charFeat => charFeat.Id == styleFeat.Id)))
        {
            player.SendServerMessage($"You have already selected {RangedStyleName} for your Fighting Style.");
            return true;
        }
        return false;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 460f, 240f)
        };
    }

    public override void Create()
    {
        InitBefore();

        if (_window == null) return;

        player.TryCreateNuiWindow(_window, out _token);
    }

    public override void Close()
    {
        _token.Close();
    }
}
