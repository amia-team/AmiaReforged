﻿using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;

public sealed class SimplePopupView : ScryView<SimplePopupPresenter>
{
    private readonly string _message;
    public sealed override SimplePopupPresenter Presenter { get; protected set; }

    public SimplePopupView(NwPlayer player, string message, string title)
    {
        _message = message;
        Presenter = new SimplePopupPresenter(player, this, title);
    }

    public override NuiLayout RootLayout()
    {
        NuiColumn popupLayout = new()
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiGroup
                        {
                            Element = new NuiLabel(_message)
                            {
                                HorizontalAlign = NuiHAlign.Center,
                                VerticalAlign = NuiVAlign.Middle
                            },
                            Border = true,
                            Width = 300f,
                            Height = 300f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiButton("OK")
                        {
                            Id = "ok_button",
                        }
                    }
                }
            }
        };
        return popupLayout;
    }
}