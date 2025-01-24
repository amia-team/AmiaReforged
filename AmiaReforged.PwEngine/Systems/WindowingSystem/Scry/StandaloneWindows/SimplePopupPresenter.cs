﻿using Anvil.API;
using Anvil.API.Events;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;

public sealed class SimplePopupPresenter : ScryPresenter<SimplePopupView>
{
    private readonly string _title;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private readonly NwPlayer _player;

    public SimplePopupPresenter(NwPlayer player, SimplePopupView view, string title)
    {
        _player = player;
        _title = title;
        View = view;
    }


    public override void HandleInput(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;
        if (obj.ElementId == "ok_button")
        {
            Close();
            return;
        }
        
        if (obj.ElementId == "ignore_button")
        {
            NwItem? pcKey = _player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey == null)
            {
                return;
            }
            
            NWScript.SetLocalInt(pcKey, "ignore_caster_forge", 1);
            Close();
        }

    }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override SimplePopupView View { get; }

    public override void Initialize()
    {
        _window = new NuiWindow(View.RootLayout(), _title)
        {
            Geometry = new NuiRect(500f, 500f, 400f, 300f),
            Resizable = false
        };
    }

    public override void UpdateView()
    {
    }

    public override void Create()
    {
        Initialize();
        _player.TryCreateNuiWindow(_window!, out _token);
        
        Token().SetBindValue(View.IgnoreButtonVisible, View.IgnoreButton);
    }

    public override void Close()
    {
        _token.Close();
    }
}