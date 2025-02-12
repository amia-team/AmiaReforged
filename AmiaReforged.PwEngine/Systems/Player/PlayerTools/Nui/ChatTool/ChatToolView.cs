using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.ChatTool;

public class ChatToolView : ScryView<ChatToolPresenter>, IToolWindow
{
    public sealed override ChatToolPresenter ToolPresenter { get; protected set; }

    public string Id => "playertools.associatechat";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Associate Chat";
    public string CategoryTag => "Character";

    public readonly NuiBind<string> ChatHistory = new("chat_history");
    public readonly NuiBind<string> ChatField = new("chat_field");
    public readonly NuiBind<string> SelectionName = new("selection_name");
    public readonly NuiBind<bool> EmphasizeSelection = new("emphasize_selection");

    public NuiButtonImage SpeakButton = null!;
    public NuiButtonImage SelectButton = null!;

    public ChatToolView(NwPlayer player)
    {
        ToolPresenter = new ChatToolPresenter(this, player);
    }

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            {
                new NuiRow()
                {
                    Children =
                    {
                        new NuiGroup()
                        {
                            Element = new NuiLabel("Talking As:")
                            {
                                VerticalAlign = NuiVAlign.Middle,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            Height = 40f,
                            Width = 200f,
                            Border = true,
                        },
                        new NuiGroup()
                        {
                            Element = new NuiLabel(SelectionName)
                            {
                                VerticalAlign = NuiVAlign.Middle,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            Height = 40f,
                            Width = 200f,
                            Border = true,
                        }
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiText(ChatHistory)
                        {
                            Height = 400f
                        },
                    }
                },
                new NuiRow()
                {
                    Children =
                    {
                        new NuiTextEdit("Enter a message", ChatField, 5000, true)
                        {
                            Height = 100,
                        },
                        new NuiColumn()
                        {
                            Children =
                            {
                                new NuiButtonImage("ir_assoc_action")
                                {
                                    Id = "select",
                                    Encouraged = EmphasizeSelection,
                                    Width = 45f,
                                    Height = 45f
                                }.Assign(out SelectButton),
                                new NuiButtonImage("ir_chat")
                                {
                                    Id = "Speak",
                                    Aspect = 1f,
                                    Width = 45f,
                                    Height = 45f,
                                }.Assign(out SpeakButton)
                            }
                        }
                    }
                },
            }
        };

        return root;
    }

    public IScryPresenter MakeWindow(NwPlayer player)
    {
        return ToolPresenter;
    }
}