using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LanguageChecker;

public class LanguageCheckerPresenter : ScryPresenter<LanguageCheckerView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private LanguageCheckerModel Model { get; }

    public LanguageCheckerPresenter(LanguageCheckerView view, NwPlayer player)
    {
        View = view;
        _player = player;
        Model = new LanguageCheckerModel(player);

        // Subscribe to character selection event
        Model.OnCharacterSelected += OnCharacterSelected;
    }

    [Inject] private Lazy<WindowDirector> WindowDirector { get; init; } = null!;

    public override LanguageCheckerView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0, 100, 500f, 550f),
            Resizable = true
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
        if (click.ElementId == View.PlayerChooserButton.Id)
        {
            Model.EnterTargetingMode();
        }
    }

    private void OnCharacterSelected(object? sender, EventArgs e)
    {
        UpdateView();
    }

    public override void UpdateView()
    {
        // Update character name
        Token().SetBindValue(View.SelectedCharacterName, Model.GetCharacterName());

        // Update language count
        string countText = $"Total Languages: {Model.GetTotalLanguageCount()}";
        Token().SetBindValue(View.LanguageCountText, countText);

        // Format automatic languages for display (one per line)
        string autoDisplay = Model.AutomaticLanguages.Count > 0
            ? string.Join(", ", Model.AutomaticLanguages)
            : "(None)";
        Token().SetBindValue(View.AutomaticLanguagesDisplay, autoDisplay);

        // Format chosen languages for display (one per line)
        string chosenDisplay = Model.ChosenLanguages.Count > 0
            ? string.Join(", ", Model.ChosenLanguages)
            : "(None)";
        Token().SetBindValue(View.ChosenLanguagesDisplay, chosenDisplay);
    }

    public override void Create()
    {
        if (_window == null) InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                "The window could not be created.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);
        UpdateView();
    }

    public override void Close()
    {
        // Cleanup if needed
    }
}

