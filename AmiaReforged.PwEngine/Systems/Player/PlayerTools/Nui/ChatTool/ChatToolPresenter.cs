using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NWN.Core;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.ChatTool;

public class ChatToolPresenter : ScryPresenter<ChatToolView>
{
    private const string TargetModeToken = "ASSOCIATE_SELECT";
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private NwPlayer _player;
    private ChatToolModel ToolModel { get; }

    public ChatToolPresenter(ChatToolView toolView, NwPlayer player)
    {
        View = toolView;
        _player = player;
        ToolModel = new ChatToolModel(player);
    }
    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override ChatToolView View { get; }
    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(500f, 100f, 430, 610f),
            Resizable = false
        };
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        switch (obj.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(obj);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent click)
    {
        if (click.ElementId == View.SpeakButton.Id)
        {
            string? chatMessage = Token().GetBindValue(View.ChatField);
            if (chatMessage.IsNullOrEmpty())
            {
                _player.SendServerMessage("You must enter a message.", ColorConstants.Orange);
                return;
            }

            ToolModel.NextMessage = chatMessage;
            ToolModel.Speak();
            Update();
            
            Token().SetBindValue(View.ChatField, ""); // Clear the chat field.
            ToolModel.NextMessage = "";
            
            return;
        }

        if (click.ElementId == View.SelectButton.Id)
        {
            _player.FloatingTextString("Pick an associate.", false);

            _player.EnterTargetMode(ValidateAndSelect, new TargetModeSettings()
            {
                CursorType = MouseCursor.Talk,
                ValidTargets = ObjectTypes.Creature
            });
        }
        
    }

    private void ValidateAndSelect(ModuleEvents.OnPlayerTarget obj)
    {
        if (obj.TargetObject is not NwCreature creature || !obj.TargetObject.IsValid)
        {
            return;
        }

        if (!ToolModel.IsAnAssociate(creature))
        {
            _player.SendServerMessage("That creature is not an associate (or yourself).", ColorConstants.Orange);
            return;
        }
        ToolModel.ChatHistory = NWScript.GetLocalString(creature, "CHAT_HISTORY");
        ToolModel.Selection = creature;
        Update();
    }

    public override void Create()
    {
        if (_window == null)
        {
            // Try to create the window if it doesn't exist.
            InitBefore();
        }

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage("The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        
        Token().SetBindValue(View.ChatHistory, ToolModel.ChatHistory);

        Token().SetBindValue(View.ChatField, "");
        Token().SetBindValue(View.SelectionName, "No selection");
        Token().SetBindValue(View.EmphasizeSelection, true);

        Update();
    }
    
    private void Update()
    {
        Token().SetBindValue(View.ChatHistory, ToolModel.ChatHistory);
        Token().SetBindValue(View.SelectionName, ToolModel.Selection?.Name ?? "Nobody");
        Token().SetBindValue(View.EmphasizeSelection, ToolModel.Selection == null);
    }

    public override void Close()
    {
        Token().Close();
    }
}