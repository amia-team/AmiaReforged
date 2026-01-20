using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Utilities.GameSettings;

public sealed class EmoteSymbolPresenter : ScryPresenter<EmoteSymbolView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public override EmoteSymbolView View { get; }
    public override NuiWindowToken Token() => _token;

    public EmoteSymbolPresenter(EmoteSymbolView view, NwPlayer player)
    {
        View = view;
        _player = player;
    }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Emote Symbol")
        {
            Geometry = new NuiRect(350f, 200f, 320f, 220f),
            Resizable = false,
            Closable = true,
            Collapsed = false
        };
    }

    public override void Create()
    {
        if (_window is null)
        {
            _player.SendServerMessage("The emote symbol window could not be created.", ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        // Get current emote symbol if set
        NwCreature? creature = _player.LoginCreature;
        if (creature != null)
        {
            string currentSymbol = creature.GetObjectVariable<LocalVariableString>("chat_emote").Value ?? "";
            Token().SetBindValue(View.SymbolInput, currentSymbol);
        }
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent ev)
    {
        if (ev.EventType != NuiEventType.Click) return;

        switch (ev.ElementId)
        {
            case "btn_set":
                HandleSetSymbol();
                break;
            case "btn_cancel":
                Close();
                break;
        }
    }

    private void HandleSetSymbol()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null)
        {
            _player.SendServerMessage("Error: Could not find your character.", ColorConstants.Red);
            return;
        }

        string symbol = Token().GetBindValue(View.SymbolInput) ?? "";

        // Validate symbol length (must be exactly 1 character)
        if (symbol.Length != 1)
        {
            _player.SendServerMessage("The emote symbol must be exactly one character!", ColorConstants.Orange);
            return;
        }

        // Check if using double-quote
        if (symbol == "\"")
        {
            _player.SendServerMessage("You are now set to denote speech with doublequotes!", ColorConstants.Cyan);
            creature.GetObjectVariable<LocalVariableInt>("chat_reverse").Value = 1; // TRUE
        }
        else
        {
            _player.SendServerMessage($"{symbol} has been set to the emote symbol!", ColorConstants.Cyan);
            creature.GetObjectVariable<LocalVariableInt>("chat_reverse").Value = 0; // FALSE
        }

        // Set the emote symbol
        creature.GetObjectVariable<LocalVariableString>("chat_emote").Value = symbol;

        Close();
    }

    public override void UpdateView()
    {
        // No dynamic updates needed
    }

    public override void Close()
    {
        _token.Close();
    }
}
