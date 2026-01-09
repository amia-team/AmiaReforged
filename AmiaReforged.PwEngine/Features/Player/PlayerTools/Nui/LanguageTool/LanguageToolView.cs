using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.LanguageTool;

public class LanguageToolView : ScryView<LanguageToolPresenter>, IToolWindow
{
    private const float WindowW = 540f;
    private const float WindowH = 580f;

    public readonly NuiBind<string> LanguageCountText = new(key: "language_count_text");
    public readonly NuiBind<int> AutomaticLanguagesCount = new(key: "automatic_languages_count");
    public readonly NuiBind<string> AutomaticLanguageLabels = new(key: "automatic_language_labels");
    public readonly NuiBind<int> ChosenLanguagesCount = new(key: "chosen_languages_count");
    public readonly NuiBind<int> AvailableLanguagesCount = new(key: "available_languages_count");
    public readonly NuiBind<string> ChosenLanguageLabels = new(key: "chosen_language_labels");
    public readonly NuiBind<string> AvailableLanguageLabels = new(key: "available_language_labels");
    public readonly NuiBind<bool> SaveButtonEnabled = new(key: "save_button_enabled");

    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage ReportLanguagesButton = null!;

    public LanguageToolView(NwPlayer player)
    {
        Presenter = new LanguageToolPresenter(this, player);
    }

    public sealed override LanguageToolPresenter Presenter { get; protected set; }

    public string Id => "playertools.languagetool";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public string Title => "Character Language Tool";
    public string CategoryTag => "Character";

    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    private NuiElement ImagePlatedButton(string id, out NuiButtonImage logicalButton,
        string plateResRef, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(plateResRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = "Save and Lock Language Selections",
            Enabled = SaveButtonEnabled
        }.Assign(out logicalButton);

        return btn;
    }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        // Languages Known label
        NuiRow languagesKnownRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 50f },
                new NuiLabel("Languages Known")
                {
                    Height = 20f,
                    Width = 440f,
                    HorizontalAlign = NuiHAlign.Center,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        // Count text row
        NuiRow countRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiLabel(LanguageCountText)
                {
                    Height = 20f,
                    Width = 480f,
                    ForegroundColor = new Color(30, 20, 12)
                }
            }
        };

        // Automatic Languages row with scrollable list (for viewing, selections don't do anything)
        NuiRow automaticRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiLabel("Automatic Languages:")
                {
                    Height = 70f,
                    Width = 150f,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },
                new NuiSpacer { Width = 15f },
                new NuiList(
                    [
                        new NuiListTemplateCell(new NuiLabel(AutomaticLanguageLabels)
                        {
                            Tooltip = "You receive these languages automatically.",
                            VerticalAlign = NuiVAlign.Middle,
                            HorizontalAlign = NuiHAlign.Center
                        }) { Width = 120f }
                    ],
                    AutomaticLanguagesCount)
                {
                    Width = 180f,
                    Height = 70f,
                    Scrollbars = NuiScrollbars.Auto
                }
            }
        };

        // List template cells for chosen languages - separate cells create columns
        List<NuiListTemplateCell> chosenLanguagesCells =
        [
            new NuiListTemplateCell(new NuiLabel(ChosenLanguageLabels)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Center
            }) { Width = 100f },
            new NuiListTemplateCell(new NuiButton("Remove")
            {
                Id = "remove_language",
                Height = 30f
            }) { Width = 80f }
        ];

        // List template cells for available languages - separate cells create columns
        List<NuiListTemplateCell> availableLanguagesCells =
        [
            new NuiListTemplateCell(new NuiLabel(AvailableLanguageLabels)
            {
                VerticalAlign = NuiVAlign.Middle,
                HorizontalAlign = NuiHAlign.Center
            }) { Width = 100f },
            new NuiListTemplateCell(new NuiButton("Add")
            {
                Id = "add_language",
                Height = 30f
            }) { Width = 80f }
        ];

        // Lists row
        NuiRow listsRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 30f },
                new NuiColumn
                {
                    Children =
                    {
                        new NuiLabel("Chosen Languages:")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiList(chosenLanguagesCells, ChosenLanguagesCount)
                        {
                            Width = 220f,
                            Height = 280f
                        }
                    }
                },
                new NuiSpacer { Width = 10f },
                new NuiColumn
                {
                    Children =
                    {
                        new NuiLabel("Available Languages:")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiList(availableLanguagesCells, AvailableLanguagesCount)
                        {
                            Width = 220f,
                            Height = 280f
                        }
                    }
                }
            }
        };

        // Save button row
        NuiRow saveButtonRow = new NuiRow
        {
            Children =
            {
                new NuiSpacer { Width = 166f },
                ImagePlatedButton("save_languages", out SaveButton, "ui_btn_save"),
                new NuiSpacer { Width = 10f },
                new NuiButtonImage("ui_speak")
                {
                    Id = "report_languages",
                    Width = 38f,
                    Height = 38f,
                    Tooltip = "Report your known languages."
                }.Assign(out ReportLanguagesButton),
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
                languagesKnownRow,
                new NuiSpacer { Height = 5f },
                countRow,
                new NuiSpacer { Height = 5f },
                automaticRow,
                new NuiSpacer { Height = 5f },
                listsRow,
                new NuiSpacer { Height = 5f },
                saveButtonRow
            }
        };

        return root;
    }
}
