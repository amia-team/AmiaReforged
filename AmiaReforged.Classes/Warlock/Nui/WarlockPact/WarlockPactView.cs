using AmiaReforged.Classes.Warlock.Types;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.Classes.Warlock.Nui.WarlockPact;

public sealed class WarlockPactView : ScryView<WarlockPactPresenter>
{
    public readonly NuiBind<PactType> PactBind = new(key: "pact_type");
    public readonly NuiBind<bool> IsConfirmViewOpen = new(key: "is_confirm_view_open");
    public readonly NuiBind<bool> CanConfirm = new(key: "can_confirm");
    public readonly NuiBind<string> PactLabel = new(key: "pact_label");
    public readonly NuiBind<string> PactIcon = new(key: "pact_icon");
    public readonly NuiBind<string> PactText = new(key: "pact_text");
    public NuiButton ConfirmPactButton = null!;
    public readonly NuiButton BackButton = null!;

    public WarlockPactView(NwPlayer player)
    {
        Presenter = new WarlockPactPresenter(this, player);
    }

    public override WarlockPactPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f,
                    Height = 0f,
                    Children = [],
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 600f, 640f))]
                },

                new NuiColumn
                {
                    Children = CreatePactRows()
                },

                new NuiColumn
                {
                    Visible = IsConfirmViewOpen,
                    Children =
                    {
                        new NuiRow
                        {
                            Children =
                            {
                                new NuiImage(PactIcon)
                                {
                                    Height = 64,
                                    Width = 64
                                },
                                new NuiLabel(PactLabel)
                                {
                                    VerticalAlign = NuiVAlign.Middle,
                                    HorizontalAlign = NuiHAlign.Center,
                                    Height = 64
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiText(PactText)
                                {
                                    Height = 280f,
                                    Scrollbars = NuiScrollbars.Y
                                }
                            }
                        },

                        new NuiRow
                        {
                            Children =
                            {
                                new NuiButton("Confirm Pact")
                                {
                                    Id = "confirm_button",
                                    Width = 120f,
                                    Visible = CanConfirm
                                }.Assign(out ConfirmPactButton),
                                new NuiSpacer()
                            }
                        }
                    }
                }
            }
        };

    private List<NuiElement> CreatePactRows()
    {
        List<NuiElement> rows = [];

        foreach (PactType pact in Enum.GetValues<PactType>())
        {
            WarlockPactModel.FeatData? pactData = WarlockPactModel.CreatePactData(pact);
            if (pactData == null)
                continue;

            rows.Add(CreatePactRow(pact, pactData.Icon, pactData.Name));
        }

        return rows;
    }

    private NuiRow CreatePactRow(PactType pact, string? icon, string name)
    {
        NuiRow row = new()
        {
            Children =
            {
                new NuiButtonImage(icon ?? string.Empty)
                {
                    Id = $"pact_{pact}",
                    Height = 64,
                    Width = 64,
                    Tooltip = name
                }
            }
        };

        Feat[] pactFeats = PactFeatMap.GetFeats(pact);
        foreach (Feat feat in pactFeats)
        {
            NwFeat? pactFeat = NwFeat.FromFeatType(feat);
            if (pactFeat is { IconResRef: not null })
            {
                row.Children.Add(new NuiButtonImage(pactFeat.IconResRef)
                {
                    Id = $"spell_{(int)feat}",
                    Height = 64,
                    Width = 64,
                    Tooltip = pactFeat.Name.ToString()
                });
            }
        }

        row.Children.Add(new NuiSpacer {Width = 400f});

        return row;
    }
}
