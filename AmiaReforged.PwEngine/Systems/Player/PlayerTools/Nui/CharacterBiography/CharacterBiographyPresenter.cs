using AmiaReforged.PwEngine.Systems.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.Player.PlayerTools.Nui.CharacterBiography;

public class CharacterBiographyPresenter : ScryPresenter<CharacterBiographyView>
{
    private NuiWindow? _window;
    private NuiWindowToken _token;
    private readonly NwPlayer _player;

    public CharacterBiographyPresenter(CharacterBiographyView toolView, NwPlayer player)
    {
        _player = player;
        View = toolView;
    }

    public override NuiWindowToken Token()
    {
        return _token;
    }

    public override CharacterBiographyView View { get; }

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(500f, 100f, 470, 560f),
            Resizable = false
        };
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

        string? characterBio = Token().Player.LoginCreature?.Description;
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
        {
            SaveCharacterBiography();
        }
        else if (eventData.ElementId == View.DiscardButton.Id)
        {
            DiscardCharacterBiography();
        }
    }

    private void SaveCharacterBiography()
    {
        string? characterBio = Token().GetBindValue(View.CharacterBiography);

        NwCreature? character = Token().Player.LoginCreature;
        if (character != null && characterBio != null)
        {
            character.Description = characterBio;
        }
        
        RaiseCloseEvent();
        Token().Player.ExportCharacter();
        Token().Close();
    }

    private void DiscardCharacterBiography()
    {
        Close();
    }
}