using AmiaReforged.Core.UserInterface;
using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.ChatTool;

public class ChatToolView : ScryView<ChatToolPresenter>, IToolWindow
{
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
    public string Title => "Player Chat Tool (Beta)";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiGroup
                        {
                            Element = new NuiLabel(label: "Speaking As:")
                            {
                                VerticalAlign = NuiVAlign.Middle,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            Height = 40f,
                            Width = 200f,
                            Border = true
                        },
                        new NuiGroup
                        {
                            Element = new NuiLabel(SelectionName)
                            {
                                VerticalAlign = NuiVAlign.Middle,
                                HorizontalAlign = NuiHAlign.Center
                            },
                            Height = 40f,
                            Width = 200f,
                            Border = true
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiText(ChatHistory)
                        {
                            Height = 400f
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiTextEdit(label: "Enter a message", ChatField, 1000, true)
                        {
                            Height = 100
                        },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiButtonImage(resRef: "ir_assoc_action")
                                {
                                    Id = "select",
                                    Encouraged = EmphasizeSelection,
                                    Width = 45f,
                                    Height = 45f
                                }.Assign(out SelectButton),
                                new NuiButtonImage(resRef: "ir_chat")
                                {
                                    Id = "Speak",
                                    Aspect = 1f,
                                    Width = 45f,
                                    Height = 45f
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