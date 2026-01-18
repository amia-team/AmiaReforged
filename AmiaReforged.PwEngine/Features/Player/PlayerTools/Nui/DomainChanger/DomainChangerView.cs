using Anvil.API;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.DomainChanger;

public sealed class DomainChangerView : IScryView
{
    public NuiBind<string> Title { get; } = new("title");
    public NuiBind<string> CharacterInfo { get; } = new("char_info");
    public NuiBind<string> CurrentDomains { get; } = new("current_domains");
    public NuiBind<string> AvailableDomains { get; } = new("available_domains");
    public NuiBind<bool> ChangeButtonsEnabled { get; } = new("change_buttons_enabled");
    public NuiBind<int> SelectedDomainSlot { get; } = new("selected_domain_slot"); // 1 or 2
    public NuiBind<int> SelectedNewDomain { get; } = new("selected_new_domain"); // Domain ID

    public NuiLayout RootLayout()
    {
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(CharacterInfo) { Height = 20f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer()
                    },
                    Height = 10f
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(CurrentDomains) { Height = 60f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer()
                    },
                    Height = 10f
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(AvailableDomains) { Height = 200f }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiSpacer()
                    },
                    Height = 10f
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Change First Domain")
                        {
                            Id = "btn_change_first_domain",
                            Enabled = ChangeButtonsEnabled,
                            Width = 200f,
                            Height = 35f
                        },
                        new NuiSpacer(),
                        new NuiButton("Change Second Domain")
                        {
                            Id = "btn_change_second_domain",
                            Enabled = ChangeButtonsEnabled,
                            Width = 200f,
                            Height = 35f
                        }
                    }
                }
            }
        };

        return root;
    }

    public NuiBind<string> ModalMessage { get; } = new("modal_message");

    public NuiWindow BuildConfirmModal(string oldDomainName, string newDomainName, int domainSlot)
    {
        NuiColumn layout = new()
        {
            Children = new List<NuiElement>
            {
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiLabel(ModalMessage)
                        {
                            Height = 80f
                        }
                    }
                },
                new NuiRow
                {
                    Children = new List<NuiElement>
                    {
                        new NuiButton("Confirm")
                        {
                            Id = "btn_domain_confirm",
                            Width = 100f,
                            Height = 35f
                        },
                        new NuiSpacer(),
                        new NuiButton("Cancel")
                        {
                            Id = "btn_domain_cancel",
                            Width = 100f,
                            Height = 35f
                        }
                    }
                }
            }
        };

        return new NuiWindow(layout, "Confirm Domain Change")
        {
            Geometry = new NuiRect(400f, 300f, 350f, 160f),
            Resizable = false
        };
    }
}
