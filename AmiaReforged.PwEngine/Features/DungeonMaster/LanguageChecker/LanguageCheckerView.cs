using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LanguageChecker;

public class LanguageCheckerView : ScryView<LanguageCheckerPresenter>, IDmWindow
{
    private const float WindowW = 425f;
    private const float WindowH = 700f;

    public readonly NuiBind<string> SelectedCharacterName = new(key: "selected_character_name");
    public readonly NuiBind<string> LanguageCountText = new(key: "language_count_text");
    public readonly NuiBind<int> AutomaticLanguagesCount = new(key: "automatic_languages_count");
    public readonly NuiBind<string> AutomaticLanguageLabels = new(key: "automatic_language_labels");
    public readonly NuiBind<int> ChosenLanguagesCount = new(key: "chosen_languages_count");
    public readonly NuiBind<string> ChosenLanguageLabels = new(key: "chosen_language_labels");
    public readonly NuiBind<int> AvailableLanguagesCount = new(key: "available_languages_count");
    public readonly NuiBind<string> AvailableLanguageLabels = new(key: "available_language_labels");
    public readonly NuiBind<string> LanguageFilterText = new(key: "language_filter_text");

    public NuiButtonImage PlayerChooserButton = null!;
    public NuiButtonImage RemoveLanguageButton = null!;
    public NuiButtonImage AddLanguageButton = null!;
    public NuiButtonImage SearchLanguagesButton = null!;

    public LanguageCheckerView(NwPlayer player)
    {
        Presenter = new LanguageCheckerPresenter(this, player);
    }

    public sealed override LanguageCheckerPresenter Presenter { get; protected set; }

    public string Id => "dmtools.languagechecker";
    public bool ListInPlayerTools => false;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Language Checker";
    public string CategoryTag => "Tools";
    public bool ListInDmTools => true;

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        // Header with player chooser button
        NuiButtonImage pickerButton = new NuiButtonImage("nui_pick")
        {
            Id = "select_character",
            Width = 35f,
            Height = 35f,
            Tooltip = "Select a player character"
        };
        PlayerChooserButton = pickerButton;

        NuiRow headerRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 20f },
                new NuiLabel("Character:")
                {
                    Height = 30f,
                    Width = 80f,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                pickerButton,
                new NuiSpacer { Width = 5f },
                new NuiLabel(SelectedCharacterName)
                {
                    Height = 30f,
                    Width = 250f,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        // Language count
        NuiRow countRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiLabel(LanguageCountText)
                {
                    Height = 20f,
                    Width = 200f,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        // Automatic languages header and display
        NuiRow automaticHeaderRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiLabel("Automatic Languages:")
                {
                    Height = 20f,
                    Width = 200f,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        NuiRow automaticDisplayRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 50f },
                new NuiList(
                    [
                        new NuiListTemplateCell(new NuiLabel(AutomaticLanguageLabels)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        }) { Width = 160f }
                    ],
                    AutomaticLanguagesCount)
                {
                    Width = 165f,
                    Height = 150f,
                    Scrollbars = NuiScrollbars.Auto
                }
            }
        };

        // Chosen languages header and display
        NuiRow chosenHeaderRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiLabel("Chosen Languages:")
                {
                    Height = 20f,
                    Width = 200f,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        NuiRow chosenDisplayRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 50f },
                new NuiList(
                    [
                        new NuiListTemplateCell(new NuiLabel(ChosenLanguageLabels)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        }) { Width = 100f },
                        new NuiListTemplateCell(new NuiButtonImage("ui_btn_sm_min")
                        {
                            Id = "remove_chosen_language",
                            Width = 20f,
                            Height = 20f,
                            Tooltip = "Remove this language from this character."
                        }) { Width = 25f }
                    ],
                    ChosenLanguagesCount)
                {
                    Width = 165f,
                    Height = 100f,
                    Scrollbars = NuiScrollbars.Auto
                }
            }
        };

        // Add language section header
        NuiRow addLanguageHeaderRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiLabel("Available Languages:")
                {
                    Height = 20f,
                    Width = 200f,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        // Add language search filter row
        NuiButtonImage searchButton = new NuiButtonImage("isk_search")
        {
            Id = "search_languages",
            Width = 35f,
            Height = 35f,
            Tooltip = "Search/filter languages"
        };
        SearchLanguagesButton = searchButton;

        NuiRow languageSearchRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 50f },
                new NuiTextEdit(label: "Filter languages...", LanguageFilterText, 50, false)
                {
                    Width = 150f,
                    Height = 35f
                },
                new NuiSpacer { Width = 5f },
                searchButton,
                new NuiSpacer { Width = 10f }
            }
        };

        // Add language list
        NuiRow addLanguageRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 50f },
                new NuiList(
                    [
                        new NuiListTemplateCell(new NuiLabel(AvailableLanguageLabels)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Left
                        }) { Width = 100f },
                        new NuiListTemplateCell(new NuiButtonImage("ui_btn_sm_plus")
                        {
                            Id = "add_available_language",
                            Width = 20f,
                            Height = 20f,
                            Tooltip = "Add this language to this character."
                        }) { Width = 25f }
                    ],
                    AvailableLanguagesCount)
                {
                    Width = 165f,
                    Height = 120f,
                    Scrollbars = NuiScrollbars.Auto
                }
            }
        };

        NuiColumn root = new()
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                new NuiSpacer { Height = 5f },
                headerRow,
                new NuiSpacer { Height = 5f },
                countRow,
                new NuiSpacer { Height = 5f },
                automaticHeaderRow,
                automaticDisplayRow,
                new NuiSpacer { Height = 3f },
                chosenHeaderRow,
                chosenDisplayRow,
                new NuiSpacer { Height = 3f },
                addLanguageHeaderRow,
                languageSearchRow,
                new NuiSpacer { Height = 3f },
                addLanguageRow
            }
        };

        return root;
    }
}

