using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterArchive;

/// <summary>
/// NUI View for the character archive/vault window.
/// </summary>
public class CharacterArchiveView : ScryView<CharacterArchiveScryPresenter>
{
    // Window dimensions - smaller to fit content properly
    public const float WindowWidth = 450f;
    public const float WindowHeight = 500f;
    public const float WindowPosX = 0f;
    public const float WindowPosY = 100f;

    // Bindings for dynamic updates
    public readonly NuiBind<string> WindowTitle = new("window_title");
    public readonly NuiBind<string> InfoText = new("info_text");
    public readonly NuiBind<string> MoveButtonLabel = new("move_button_label");
    public readonly NuiBind<string> PageInfo = new("page_info");
    public readonly NuiBind<bool> ShowPrevPage = new("show_prev_page");
    public readonly NuiBind<bool> ShowNextPage = new("show_next_page");

    // Individual character row bindings (10 characters per page)
    public readonly List<NuiBind<string>> CharacterNames = new();
    public readonly List<NuiBind<string>> CharacterPortraits = new();
    public readonly List<NuiBind<bool>> CharacterRowVisible = new();

    public CharacterArchiveView()
    {
        // Initialize bindings for 10 characters per page
        for (int i = 0; i < 10; i++)
        {
            CharacterNames.Add(new NuiBind<string>($"char_name_{i}"));
            CharacterPortraits.Add(new NuiBind<string>($"char_portrait_{i}"));
            CharacterRowVisible.Add(new NuiBind<bool>($"char_visible_{i}"));
        }
    }

    public override CharacterArchiveScryPresenter Presenter { get; protected set; } = null!;

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                // Background image
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = new List<NuiElement>(),
                    DrawList = new List<NuiDrawListItem>
                    {
                        new NuiDrawListImage("ui_bg", new NuiRect(-20f, -20f, 490, 790))
                    }
                },

                // Title
                new NuiLabel(WindowTitle)
                {
                    Height = 30f,
                    HorizontalAlign = NuiHAlign.Center,
                    VerticalAlign = NuiVAlign.Middle,
                    ForegroundColor = new Color(30, 20, 12)
                },

                // Tab buttons
                new NuiRow
                {
                    Height = 40f,
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiButton("Vault")
                        {
                            Id = "btn_show_vault",
                            Height = 35f
                        },
                        new NuiButton("Archive")
                        {
                            Id = "btn_show_archive",
                            Height = 35f
                        }
                    }
                },

                // Info text
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 120f },
                        new NuiLabel(InfoText)
                        {
                            Height = 25f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },

                // Page navigation
                new NuiRow
                {
                    Height = 35f,
                    Children = new List<NuiElement>
                    {
                        new NuiButton("< Previous")
                        {
                            Id = "btn_prev_page",
                            Width = 100f,
                            Height = 30f,
                            Visible = ShowPrevPage
                        },
                        new NuiLabel(PageInfo)
                        {
                            Width = 100f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiButton("Next >")
                        {
                            Id = "btn_next_page",
                            Width = 100f,
                            Height = 30f,
                            Visible = ShowNextPage
                        }
                    }
                },

                // Character list - build individual rows for each character
                new NuiColumn
                {
                    Height = 550f,
                    Children = BuildCharacterRows()
                },

                // Cancel button (image button)
                new NuiButtonImage("ui_btn_cancel")
                {
                    Id = "btn_close",
                    Width = 150f,
                    Height = 38f,
                    Tooltip = "Cancel and close window"
                }
            }
        };

        return root;
    }

    private List<NuiElement> BuildCharacterRows()
    {
        List<NuiElement> rows = new();

        for (int i = 0; i < 10; i++)
        {
            rows.Add(new NuiRow
            {
                Height = 50f,
                Visible = CharacterRowVisible[i],
                Children = new List<NuiElement>
                {
                    new NuiSpacer { Width = 5f },
                    // Portrait
                    new NuiImage(CharacterPortraits[i])
                    {
                        Width = 32f,
                        Height = 40f,
                        ImageAspect = NuiAspect.Fit,
                        VerticalAlign = NuiVAlign.Middle
                    },
                    new NuiSpacer { Width = 10f },
                    // Character name
                    new NuiLabel(CharacterNames[i])
                    {
                        Width = 220f,
                        Height = 40f,
                        HorizontalAlign = NuiHAlign.Left,
                        VerticalAlign = NuiVAlign.Middle,
                        ForegroundColor = new Color(30, 20, 12)
                    },
                    new NuiSpacer { Width = 10f },
                    // Move button
                    new NuiButton(MoveButtonLabel)
                    {
                        Id = $"btn_move_{i}",
                        Width = 100f,
                        Height = 35f
                    },
                    new NuiSpacer { Width = 5f }
                }
            });
        }

        return rows;
    }
}
