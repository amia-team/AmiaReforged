using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.LanguageTool;

public class LanguageToolPresenter : ScryPresenter<LanguageToolView>
{
    private readonly NwPlayer _player;
    private NuiWindowToken _token;
    private NuiWindow? _window;
    private bool _pendingConfirmation;

    public LanguageToolPresenter(LanguageToolView view, NwPlayer player)
    {
        View = view;
        _player = player;
        Model = new LanguageToolModel(player);

        NwCreature? character = player.LoginCreature;

        if (character == null)
        {
            player.SendServerMessage("Character could not be found. Please relog.", ColorConstants.Orange);
        }
    }

    [Inject] private Lazy<WindowDirector> WindowDirector { get; init; } = null!;

    private LanguageToolModel Model { get; }

    public override LanguageToolView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), View.Title)
        {
            Geometry = new NuiRect(0, 100, 540f, 580f),
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
        if (click.ElementId == View.SaveButton.Id)
        {
            ShowSaveConfirmation();
            return;
        }

        if (click.ElementId == "add_language")
        {
            if (click.ArrayIndex >= 0 && click.ArrayIndex < Model.AvailableLanguages.Count)
            {
                string language = Model.AvailableLanguages[click.ArrayIndex];
                if (Model.AddLanguage(language))
                {
                    _player.SendServerMessage($"Added language: {language}", ColorConstants.Green);
                    UpdateView();
                }
                else
                {
                    _player.SendServerMessage(
                        "Could not add language. You may have reached your maximum.",
                        ColorConstants.Orange);
                }
            }

            return;
        }

        if (click.ElementId == "remove_language")
        {
            if (click.ArrayIndex >= 0 && click.ArrayIndex < Model.ChosenLanguages.Count)
            {
                string language = Model.ChosenLanguages[click.ArrayIndex];
                if (Model.RemoveLanguage(language))
                {
                    _player.SendServerMessage($"Removed language: {language}", ColorConstants.Green);
                    UpdateView();
                }
                else
                {
                    // Check if it's a saved language
                    if (Model.SavedLanguages.Contains(language))
                    {
                        _player.SendServerMessage(
                            $"Cannot remove '{language}' - it was previously saved. Contact a DM if you need to change saved languages.",
                            ColorConstants.Orange);
                    }
                }
            }
        }
    }

    private void ShowSaveConfirmation()
    {
        if (_pendingConfirmation)
        {
            return; // Already showing confirmation
        }

        _pendingConfirmation = true;

        string message = Model.IsLocked
            ? $"You currently have {Model.ChosenLanguages.Count} out of {Model.MaxChoosableLanguages} available languages.\n\n" +
              "Confirm below to add the new selections to your saved languages.\n\n" +
              "You can continue adding languages until you reach your maximum, but you cannot remove languages once saved."
            : $"You have chosen {Model.ChosenLanguages.Count} out of {Model.MaxChoosableLanguages} available languages.\n\n" +
              "Confirm below to save these selections.\n\n" +
              "WARNING: Once saved, you CANNOT REMOVE these language selections without RH or DM aid, but you can later add more up to your maximum allotment.";

        LanguageConfirmationView confirmPopup = new LanguageConfirmationView(
            _player,
            OnConfirmSave,
            OnConfirmationCancelled,
            message
        );

        // Create the popup directly - it will handle its own events
        confirmPopup.Presenter.Create();
    }

    private void OnConfirmSave()
    {
        _pendingConfirmation = false;

        if (Model.SaveLanguages())
        {
            UpdateView();
            _player.SendServerMessage(
                "Your language selections have been saved to your character!",
                ColorConstants.Green);
        }
        else
        {
            _player.SendServerMessage(
                "Failed to save languages. Please contact a Request Handler.",
                ColorConstants.Red);
        }
    }

    private void OnConfirmationCancelled()
    {
        _pendingConfirmation = false;
    }

    public override void UpdateView()
    {
        // Update language count text
        string countText = $"You have chosen {Model.ChosenLanguages.Count} out of {Model.MaxChoosableLanguages} languages your character can know.";
        Token().SetBindValue(View.LanguageCountText, countText);

        // Update automatic languages list (remove any empty strings)
        List<string> autoLanguages = Model.AutomaticLanguages.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        Token().SetBindValues(View.AutomaticLanguageLabels, autoLanguages);
        Token().SetBindValue(View.AutomaticLanguagesCount, autoLanguages.Count);

        // Update chosen languages list
        Token().SetBindValues(View.ChosenLanguageLabels, Model.ChosenLanguages);
        Token().SetBindValue(View.ChosenLanguagesCount, Model.ChosenLanguages.Count);

        // Update available languages list
        Token().SetBindValues(View.AvailableLanguageLabels, Model.AvailableLanguages);
        Token().SetBindValue(View.AvailableLanguagesCount, Model.AvailableLanguages.Count);

        // Save button is always enabled - can save at any time with any number of languages
        Token().SetBindValue(View.SaveButtonEnabled, true);
    }

    public override void Create()
    {
        if (_window == null) InitBefore();

        if (_window == null)
        {
            _player.SendServerMessage(
                "The window could not be created. Screenshot this message and report it to a DM.",
                ColorConstants.Orange);
            return;
        }

        _player.TryCreateNuiWindow(_window, out _token);

        UpdateView();
    }

    public override void Close()
    {
        _pendingConfirmation = false;
    }
}

