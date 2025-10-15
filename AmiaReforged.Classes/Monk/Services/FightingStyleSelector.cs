﻿using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Nui.FightingStyle;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Monk.Services;

[ServiceBinding(typeof(FightingStyleSelector))]
public class FightingStyleSelector
{
    private readonly WindowDirector _windowManager;

    public FightingStyleSelector(WindowDirector windowManager)
    {
        _windowManager = windowManager;

        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");
        if (environment == "live") return;

        NwModule.Instance.OnUseFeat += OpenPathSelectorWindow;
    }

    private void OpenPathSelectorWindow(OnUseFeat eventData)
    {
        if (eventData.Feat.Id is not MonkFeat.MonkFightingStyle) return;
        if (!eventData.Creature.IsPlayerControlled(out NwPlayer? player)) return;

        if (_windowManager.IsWindowOpen(player, typeof(FightingStylePresenter)))
        {
            player.FloatingTextString(message: "Fighting Style selection is already open.", false);
            return;
        }

        FightingStyleView window = new(player);
        FightingStylePresenter presenter = window.Presenter;

        _windowManager.OpenWindow(presenter);
    }
}
