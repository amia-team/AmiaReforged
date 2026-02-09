using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
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
        { RangedStyleName, [NwFeat.FromFeatType(Feat.CalledShot)!, NwFeat.FromFeatType(Feat.ZenArchery)!] }
    };

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click)
            HandleButtonClick(eventData);
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        int token = eventData.Token.Token;

        if (eventData.ElementId == View.KnockdownStyleButton.Id ||
            eventData.ElementId == View.DisarmStyleButton.Id ||
            eventData.ElementId == View.RangedStyleButton.Id)
        {
            View.IsKnockdownSelected.SetBindValue(player, token, eventData.ElementId == View.KnockdownStyleButton.Id);
            View.IsDisarmSelected.SetBindValue(player, token, eventData.ElementId == View.DisarmStyleButton.Id);
            View.IsRangedSelected.SetBindValue(player, token, eventData.ElementId == View.RangedStyleButton.Id);

            View.ShowConfirm.SetBindValue(player, token, true);
            return;
        }

        if (eventData.ElementId == View.ConfirmButton.Id)
        {
            if (View.IsKnockdownSelected.GetBindValue(player, token))
            {
                ChooseStyle(KnockdownStyleName);
            }
            else if (View.IsDisarmSelected.GetBindValue(player, token))
            {
                ChooseStyle(DisarmStyleName);
            }
            else if (View.IsRangedSelected.GetBindValue(player, token))
            {
                ChooseStyle(RangedStyleName);
            }
        }
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

        NwFeat? fightingStyleFeat = monk.Feats.FirstOrDefault(f => f.Id == MonkFeat.MonkFightingStyle);
        if (fightingStyleFeat == null)
        {
            player.SendServerMessage("Could not find the feat required to open the selection window!");
            return;
        }

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        if (monkLevel < 6)
        {
            player.SendServerMessage("You need to be level 6 to learn your Fighting Style.");
            return;
        }

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

        RaiseCloseEvent();

        player.FloatingTextString($"Added feats {featsToAdd[0].Name} and {featsToAdd[1].Name}", false);

        // NB! Do not remove this from the feat list on removal. This is crucial so that the MonkValidator doesn't
        // keep adding this feat over and over again on character login
        monk.RemoveFeat(fightingStyleFeat);
    }

    private bool HasMonkFightingStyle(NwCreature monk)
    {
        int monkLevelCount = 0;
        CreatureLevelInfo? monkLevelSixInfo = null;

        foreach (CreatureLevelInfo levelInfo in monk.LevelInfo)
        {
            if (levelInfo.ClassInfo.Class.ClassType != ClassType.Monk) continue;

            monkLevelCount++;
            if (monkLevelCount != 6) continue;

            monkLevelSixInfo = levelInfo;
            break;
        }

        if (monkLevelSixInfo == null) return false;

        foreach (var style in FightingStyleFeats)
        {
            if (style.Value.All(styleFeat => monkLevelSixInfo.Feats.Any(f => f.Id == styleFeat.Id)))
            {
                monk.ControllingPlayer?.SendServerMessage($"You already selected {style.Key} at Monk Level 6.");
                return true;
            }
        }
        return false;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), WindowTitle)
        {
            Geometry = new NuiRect(400f, 400f, 460f, 300f)
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
