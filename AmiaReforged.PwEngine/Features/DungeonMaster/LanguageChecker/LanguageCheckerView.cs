using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.LanguageChecker;

public class LanguageCheckerView : ScryView<LanguageCheckerPresenter>, IDmWindow
{
    private const float WindowW = 500f;
    private const float WindowH = 520f;

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
                new NuiSpacer { Width = 30f },
                pickerButton,
                new NuiSpacer { Width = 5f },
                new NuiLabel(SelectedCharacterName)
                {
                    Height = 30f,
                    Width = 400f,
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

        // Combined header row for Automatic and Chosen languages
        NuiRow languagesHeaderRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiLabel("Automatic Languages:")
                {
                    Height = 20f,
                    Width = 160f,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Width = 45f },
                new NuiLabel("Chosen Languages:")
                {
                    Height = 20f,
                    Width = 160f,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        // Combined display row for Automatic and Chosen languages lists
        NuiRow languagesDisplayRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiList(
                    [
                        new NuiListTemplateCell(new NuiLabel(AutomaticLanguageLabels)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center
                        }) { Width = 160f }
                    ],
                    AutomaticLanguagesCount)
                {
                    Width = 195f,
                    Height = 150f,
                    Scrollbars = NuiScrollbars.Auto
                },
                new NuiSpacer { Width = 10f },
                new NuiList(
                    [
                        new NuiListTemplateCell(new NuiLabel(ChosenLanguageLabels)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center
                        }) { Width = 130f },
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
                    Width = 195f,
                    Height = 150f,
                    Scrollbars = NuiScrollbars.Auto
                },
                new NuiSpacer { Width = 30f }
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

        // Add language search button
        NuiButtonImage searchButton = new NuiButtonImage("isk_search")
        {
            Id = "search_languages",
            Width = 35f,
            Height = 35f,
            Tooltip = "Search/filter languages"
        };
        SearchLanguagesButton = searchButton;

        // Available languages list with filter on the same row
        NuiRow addLanguageRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiList(
                    [
                        new NuiListTemplateCell(new NuiLabel(AvailableLanguageLabels)
                        {
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center
                        }) { Width = 130f },
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
                    Width = 195f,
                    Height = 120f,
                    Scrollbars = NuiScrollbars.Auto
                },
                new NuiSpacer { Width = 10f },
                new NuiTextEdit(label: "Filter...", LanguageFilterText, 50, false)
                {
                    Width = 130f,
                    Height = 35f
                },
                new NuiSpacer { Width = 5f },
                searchButton,
                new NuiSpacer { Width = 10f }
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
                languagesHeaderRow,
                languagesDisplayRow,
                new NuiSpacer { Height = 10f },
                addLanguageHeaderRow,
                new NuiSpacer { Height = 3f },
                addLanguageRow,
                new NuiSpacer { Height = 10f },
            }
        };

        return root;
    }
}

