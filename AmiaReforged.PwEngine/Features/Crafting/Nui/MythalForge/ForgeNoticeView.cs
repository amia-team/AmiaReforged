using Anvil.API;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Crafting.Nui.MythalForge;

/// <summary>
/// Custom view for forge notice popups in Mythal Forge, matching the forge styling.
/// </summary>
public sealed class ForgeNoticeView : ScryView<ForgeNoticePresenter>
{
    private readonly string _title;
    private readonly string _message;
    private readonly Color _titleColor;

    public ForgeNoticeView(NwPlayer player, string title, string message, bool isError = false)
    {
        _title = title;
        _message = message;
        _titleColor = isError ? new Color(200, 32, 32) : new Color(220, 180, 100);
        Presenter = new ForgeNoticePresenter(player, this, title);
    }

    public override ForgeNoticePresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn layout = new()
        {
            Width = 480f,
            Height = 180f,
            Children =
            {
                // Background image
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    DrawList = new()
                    {
                        new NuiDrawListImage("ui_forge", new NuiRect(-20f, -270f, 520f, 560f))
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiLabel($"==={_title}===")
                {
                    Width = 450f,
                    Height = 22f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = _titleColor
                },
                new NuiSpacer { Height = 5f },
                new NuiLabel(_message)
                {
                    Width = 450f,
                    Height = 20f,
                    HorizontalAlign = NuiHAlign.Center
                },
                new NuiSpacer { Height = 15f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 165f },
                        new NuiButton("OK") { Id = "ok_button", Width = 120f, Height = 38f, Encouraged = true }
                    }
                }
            }
        };

        return layout;
    }
}
