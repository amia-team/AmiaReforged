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
            Geometry = new NuiRect(0, 100, 425f, 700f),
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
            return;
        }

        if (click.ElementId == View.SearchLanguagesButton.Id)
        {
            // Get the filter text from the bind and apply it
            string filterText = Token().GetBindValue(View.LanguageFilterText) ?? string.Empty;
            Model.SetLanguageFilter(filterText);
            UpdateView();
            return;
        }

        if (click.ElementId == "remove_chosen_language")
        {
            // Combined list is: chosen languages + DM-added languages
            int totalLanguages = Model.ChosenLanguages.Count + Model.DmAddedLanguages.Count;
            if (click.ArrayIndex >= 0 && click.ArrayIndex < totalLanguages)
            {
                string language;
                if (click.ArrayIndex < Model.ChosenLanguages.Count)
                {
                    // It's a chosen language
                    language = Model.ChosenLanguages[click.ArrayIndex];
                }
                else
                {
                    // It's a DM-added language, need to remove the " (DM)" suffix to get the real name
                    int dmIndex = click.ArrayIndex - Model.ChosenLanguages.Count;
                    language = Model.DmAddedLanguages[dmIndex];
                }

                Model.RemoveChosenLanguage(language);
                UpdateView();
                _player.SendServerMessage($"Removed '{language}' from {Model.GetCharacterName()}", ColorConstants.Green);
            }
            return;
        }


        if (click.ElementId == "add_available_language")
        {
            if (click.ArrayIndex >= 0)
            {
                var availableLanguages = Model.GetAvailableLanguages();
                if (click.ArrayIndex < availableLanguages.Count)
                {
                    string language = availableLanguages[click.ArrayIndex];
                    Model.AddChosenLanguage(language);
                    UpdateView();
                    _player.SendServerMessage($"Added '{language}' to {Model.GetCharacterName()}", ColorConstants.Green);
                }
            }
            return;
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

        // Update automatic languages list
        Token().SetBindValues(View.AutomaticLanguageLabels, Model.AutomaticLanguages);
        Token().SetBindValue(View.AutomaticLanguagesCount, Model.AutomaticLanguages.Count);

        // Update chosen languages list (combine chosen + DM-added for display)
        List<string> allChosenLanguages = new List<string>(Model.ChosenLanguages);
        // Add DM-added languages with " (DM)" suffix to distinguish them
        allChosenLanguages.AddRange(Model.DmAddedLanguages.Select(lang => $"{lang} (DM)"));
        Token().SetBindValues(View.ChosenLanguageLabels, allChosenLanguages);
        Token().SetBindValue(View.ChosenLanguagesCount, allChosenLanguages.Count);

        // Update available languages list
        var availableLanguages = Model.GetAvailableLanguages();
        Token().SetBindValues(View.AvailableLanguageLabels, availableLanguages);
        Token().SetBindValue(View.AvailableLanguagesCount, availableLanguages.Count);
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
        Token().SetBindValue(View.LanguageFilterText, string.Empty);
        UpdateView();
    }

    public override void Close()
    {
        // Cleanup if needed
    }
}

