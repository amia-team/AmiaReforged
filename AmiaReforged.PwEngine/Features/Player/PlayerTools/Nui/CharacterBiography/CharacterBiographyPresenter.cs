using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterBiography;

public class CharacterBiographyPresenter : ScryPresenter<CharacterBiographyView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private string? _originalBio;

    public CharacterBiographyPresenter(CharacterBiographyView toolView, NwPlayer player)
    {
        _player = player;
        View = toolView;
    }

    public override CharacterBiographyView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0f, 100f, 670, 590f),
            Resizable = false
        };
    }

    public override void Create()
    {
        if (_window == null)
            // Try to create the window if it doesn't exist.
            InitBefore();

        // If the window wasn't created, then tell the user we screwed up.
        if (_window == null)
        {
            _player.SendServerMessage(
                message: "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        string? characterBio = Token().Player.LoginCreature?.Description;
        _originalBio = characterBio;
        Token().SetBindValue(View.CharacterBiography!, characterBio);
    }

    public override void Close()
    {
        Token().Close();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        switch (eventData.EventType)
        {
            case NuiEventType.Click:
                HandleButtonClick(eventData);
                break;
        }
    }

    private void HandleButtonClick(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.ElementId == View.SaveButton.Id)
            SaveCharacterBiography();
        else if (eventData.ElementId == View.DiscardButton.Id)
            DiscardChanges();
        else if (eventData.ElementId == View.CancelButton.Id)
            CancelAndClose();
    }

    private void SaveCharacterBiography()
    {
        string? characterBio = Token().GetBindValue(View.CharacterBiography);

        NwCreature? character = Token().Player.LoginCreature;
        if (character != null && characterBio != null)
        {
            character.Description = characterBio;
            _originalBio = characterBio; // Update original bio after saving
        }

        Token().Player.ExportCharacter();
        Token().Player.SendServerMessage("Biography saved successfully.", ColorConstants.Green);
    }

    private void DiscardChanges()
    {
        // Revert to original bio without closing the window
        Token().SetBindValue(View.CharacterBiography!, _originalBio);
        Token().Player.SendServerMessage("Changes discarded.", ColorConstants.Orange);
    }

    private void CancelAndClose()
    {
        // Revert to original bio and close the window
        NwCreature? character = Token().Player.LoginCreature;
        if (character != null && _originalBio != null)
        {
            character.Description = _originalBio;
        }
        Close();
    }
}
