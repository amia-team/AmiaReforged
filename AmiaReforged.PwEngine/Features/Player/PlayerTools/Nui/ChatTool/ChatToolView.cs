using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.ChatTool;

public class ChatToolView : ScryView<ChatToolPresenter>, IToolWindow
{
    private const float WindowW = 630f;
    private const float WindowH = 650f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 0f;
    private const float HeaderLeftPad = 5f;

    public readonly NuiBind<string> ChatField = new(key: "chat_field");

    public readonly NuiBind<string> ChatHistory = new(key: "chat_history");
    public readonly NuiBind<bool> EmphasizeSelection = new(key: "emphasize_selection");
    public readonly NuiBind<string> SelectionName = new(key: "selection_name");
    public NuiButtonImage SelectButton = null!;

    public NuiButtonImage SpeakButton = null!;

    public ChatToolView(NwPlayer player)
    {
        Presenter = new ChatToolPresenter(this, player);
    }

    public sealed override ChatToolPresenter Presenter { get; protected set; }

    public string Id => "playertools.associatechat";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Player Chat Tool";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            {
                new NuiRow { Width = 0f, Height = 0f, DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))] },
                new NuiRow { Width = 0f, Height = 0f, DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))] },
                new NuiSpacer { Height = 85f },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 205f },
                        new NuiLabel(label: "Speaking As:")
                        {
                            Width = 90f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 10f },
                        new NuiLabel(SelectionName)
                        {
                            Width = 200f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },

                new NuiSpacer { Height = 8f },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 120f },
                        new NuiText(ChatHistory) { Width = 400f, Height = 300f }
                    }
                },

                new NuiSpacer { Height = 8f },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 120f },
                        new NuiTextEdit(label: "Enter a message", ChatField, 1000, true) { Width = 400f, Height = 105f },
                        new NuiSpacer { Width = 10f },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiButtonImage(resRef: "nui_pick")
                                {
                                    Id = "select",
                                    Width = 45f,
                                    Height = 45f,
                                    Tooltip = "Choose a Speaker"
                                }.Assign(out SelectButton),
                                new NuiSpacer { Height = 5f },
                                new NuiButtonImage(resRef: "ui_speak")
                                {
                                    Id = "Speak",
                                    Width = 45f,
                                    Height = 45f,
                                    Tooltip = "Speak"
                                }.Assign(out SpeakButton)
                            }
                        }
                    }
                }
            }
        };

        return root;
    }
}
