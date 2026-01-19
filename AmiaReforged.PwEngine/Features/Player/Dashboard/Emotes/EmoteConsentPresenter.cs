using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public sealed class EmoteConsentPresenter : ScryPresenter<EmoteConsentView>
{
    private readonly NwPlayer _targetPlayer;
    private readonly NwPlayer _requesterPlayer;
    private readonly NwCreature _requesterCreature;
    private readonly EmoteOption _emote;
    private readonly Action<bool> _onResponse;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public EmoteConsentPresenter(
        EmoteConsentView view,
        NwPlayer targetPlayer,
        NwPlayer requesterPlayer,
        NwCreature requesterCreature,
        EmoteOption emote,
        Action<bool> onResponse)
    {
        View = view;
        _targetPlayer = targetPlayer;
        _requesterPlayer = requesterPlayer;
        _requesterCreature = requesterCreature;
        _emote = emote;
        _onResponse = onResponse;
    }

    public override EmoteConsentView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Emote Consent")
        {
            Geometry = new NuiRect(300f, 200f, 300f, 150f),
            Resizable = false,
            Closable = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _targetPlayer.SendServerMessage(
                "The consent window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _targetPlayer.TryCreateNuiWindow(_window, out _token);

        // Set the consent message
        string requesterName = _requesterCreature.Name;
        string emoteName = _emote.Name.ToLower();
        string message = $"{requesterName} would like to {emoteName} with you. Do you consent?";

        Token().SetBindValue(View.ConsentMessage, message);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent obj)
    {
        if (obj.EventType != NuiEventType.Click) return;

        switch (obj.ElementId)
        {
            case "btn_consent_yes":
                HandleResponse(true);
                break;
            case "btn_consent_no":
                HandleResponse(false);
                break;
        }
    }

    private void HandleResponse(bool consented)
    {
        if (consented)
        {
            _targetPlayer.SendServerMessage($"You consented to {_emote.Name} with {_requesterCreature.Name}.", ColorConstants.Green);
            _requesterPlayer.SendServerMessage($"{_targetPlayer.LoginCreature?.Name} consented to {_emote.Name}.", ColorConstants.Green);
        }
        else
        {
            _targetPlayer.SendServerMessage($"You declined {_emote.Name} with {_requesterCreature.Name}.", ColorConstants.Orange);
            _requesterPlayer.SendServerMessage($"{_targetPlayer.LoginCreature?.Name} declined your {_emote.Name} request.", ColorConstants.Orange);
        }

        // Call the response callback
        _onResponse.Invoke(consented);

        // Close the consent window
        Close();
    }

    public override void UpdateView()
    {
        // Nothing to update dynamically
    }

    public override void Close()
    {
        _token.Close();
    }
}
