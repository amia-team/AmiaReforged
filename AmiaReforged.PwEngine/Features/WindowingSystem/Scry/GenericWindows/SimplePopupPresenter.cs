﻿using Anvil.API;
using Anvil.API.Events;
using NWN.Core;
using Action = System.Action;

namespace AmiaReforged.PwEngine.Features.WindowingSystem.Scry.GenericWindows;

public sealed class SimplePopupPresenter : ScryPresenter<SimplePopupView>
{
    private readonly NwPlayer _player;
    private readonly string _title;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    private Action? Outcome { get; set; }
    public SimplePopupPresenter(NwPlayer player, SimplePopupView toolView, string title)
    {
        _player = player;
        _title = title;
        View = toolView;
    }

    public SimplePopupPresenter(NwPlayer player, SimplePopupView toolView, Action outcome, string title)
    {
        _player = player;
        _title = title;
        View = toolView;
        Outcome = outcome;
    }

    public override SimplePopupView View { get; }


    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;
        if (obj.ElementId == "ok_button")
        {
            Close();

            Outcome?.Invoke();

            return;
        }

        if (obj.ElementId == "ignore_button")
        {
            NwItem? pcKey = _player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey == null) return;

            NWScript.SetLocalInt(pcKey, sVarName: "ignore_caster_forge", 1);
            Close();
        }
    }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), _title)
        {
            Geometry = new NuiRect(500f, 500f, 400f, 300f),
            Resizable = true
        };
    }

    public override void UpdateView()
    {
        // No updates needed
    }

    public override void Create()
    {
        InitBefore();
        _player.TryCreateNuiWindow(_window!, out _token);

        Token().SetBindValue(View.IgnoreButtonVisible, View.IgnoreButton);
    }

    public override void Close()
    {
        _token.Close();
    }
}
