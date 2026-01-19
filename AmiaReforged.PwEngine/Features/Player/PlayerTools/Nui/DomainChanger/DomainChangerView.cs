using Anvil.API;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DomainChanger;

public sealed class DomainChangerView : IScryView
{
    public NuiBind<string> Title { get; } = new("title");
    public NuiBind<string> CharacterName { get; } = new("char_name");
    public NuiBind<string> DeityName { get; } = new("deity_name");
    public NuiBind<string> Domain1Name { get; } = new("domain1_name");
    public NuiBind<string> Domain2Name { get; } = new("domain2_name");
    public NuiBind<int> SelectedDomainIndex { get; } = new("selected_domain_index");
    public NuiBind<List<NuiComboEntry>> DomainOptions { get; } = new("domain_options");
    public NuiBind<bool> ChangeButtonsEnabled { get; } = new("change_buttons_enabled");
    public NuiBind<string> ErrorMessage { get; } = new("error_message");
    public NuiBind<bool> ShowError { get; } = new("show_error");

    public NuiLayout RootLayout()
    {
        // Background layer
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 500f, 380f)) }
        };

        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                bgLayer,
                new NuiSpacer { Height = 20f },
                // Character Label
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel("Character:") { Width = 100f, Height = 20f, ForegroundColor = new Color(30, 20, 12) },
                        new NuiLabel(CharacterName) { Height = 20f, ForegroundColor = new Color(30, 20, 12) }
                    }
                },
                // Deity Label
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel("Deity:") { Width = 100f, Height = 20f, ForegroundColor = new Color(30, 20, 12) },
                        new NuiLabel(DeityName) { Height = 20f, ForegroundColor = new Color(30, 20, 12) }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer()
                    },
                    Height = 5f
                },
                // Current Domains Header
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel("Current Domains:") { Height = 20f, ForegroundColor = new Color(30, 20, 12) }
                    }
                },
                // Domain 1
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel("  Domain 1:") { Width = 100f, Height = 20f, ForegroundColor = new Color(30, 20, 12) },
                        new NuiLabel(Domain1Name) { Height = 20f, ForegroundColor = new Color(30, 20, 12) }
                    }
                },
                // Domain 2
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel("  Domain 2:") { Width = 100f, Height = 20f, ForegroundColor = new Color(30, 20, 12) },
                        new NuiLabel(Domain2Name) { Height = 20f, ForegroundColor = new Color(30, 20, 12) }
                    }
                },
                // Domain Selection Dropdown
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel("Select New Domain:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)

                        },
                        new NuiCombo
                        {
                            Entries = DomainOptions,
                            Selected = SelectedDomainIndex,
                            Width = 120f,
                            Height = 30f,
                            Enabled = ChangeButtonsEnabled
                        },
                        new NuiButton("Domain 1")
                        {
                            Id = "btn_change_domain_1",
                            Enabled = ChangeButtonsEnabled,
                            Tooltip = "Change Domain 1",
                            Width = 80f,
                            Height = 30f
                        },
                        new NuiButton("Domain 2")
                        {
                            Id = "btn_change_domain_2",
                            Enabled = ChangeButtonsEnabled,
                            Tooltip = "Change Domain 2",
                            Width = 80f,
                            Height = 30f
                        }
                    }
                },
                // Error message (conditionally shown)
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer { Width = 30f },
                        new NuiLabel(ErrorMessage) { Height = 40f, ForegroundColor = new Color(30, 20, 12) }
                    },
                    Visible = ShowError
                },
            }
        };

        return root;
    }
}
