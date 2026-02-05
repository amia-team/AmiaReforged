﻿﻿using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using static AmiaReforged.Classes.Monk.Nui.FightingStyle.FightingStyleNuiElements;

namespace AmiaReforged.Classes.Monk.Nui.FightingStyle;

public sealed class FightingStyleView : ScryView<FightingStylePresenter>
{
    public NuiButtonSelect KnockdownStyleButton = null!;
    public NuiButtonSelect DisarmStyleButton = null!;
    public NuiButtonSelect RangedStyleButton = null!;
    public NuiButton ConfirmButton = null!;

    public readonly NuiBind<bool> IsKnockdownSelected = new("sel_knockdown");
    public readonly NuiBind<bool> IsDisarmSelected = new("sel_disarm");
    public readonly NuiBind<bool> IsRangedSelected = new("sel_ranged");

    // Bind to show/hide the master confirm button
    public readonly NuiBind<bool> ShowConfirm = new("show_confirm");

    public FightingStyleView(NwPlayer player)
    {
        Presenter = new FightingStylePresenter(this, player);
    }

    public override FightingStylePresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout() =>
        new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = 0f, Height = 0f, Children = [],
                    DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 500f, 400f))]
                },

                CreateStyleRow(KnockdownStyleName, KnockdownStyleDescription, "btn_knockdown",
                    IsKnockdownSelected, out KnockdownStyleButton),

                CreateStyleRow(DisarmStyleName, DisarmStyleDescription, "btn_disarm",
                    IsDisarmSelected, out DisarmStyleButton),

                CreateStyleRow(RangedStyleName, RangedStyleDescription, "btn_ranged",
                    IsRangedSelected, out RangedStyleButton),

                new NuiRow
                {
                    Children =
                    {
                        new NuiButton("Confirm Selection")
                        {
                            Id = "btn_confirm_master",
                            Height = 40f,
                            Visible = ShowConfirm
                        }.Assign(out ConfirmButton)
                    }
                }
            }
        };

    private NuiRow CreateStyleRow(string name, string tooltip, string id, NuiBind<bool> bind, out NuiButtonSelect button)
    {
        return new NuiRow
        {
            Height = 60f,
            Children =
            {
                new NuiButtonSelect(name, bind)
                {
                    Id = id,
                    Width = 120f
                }.Assign(out button),
                new NuiText(tooltip)
                {
                    Border = false,
                    Scrollbars = NuiScrollbars.None,
                    ForegroundColor = new Color(50, 40, 30)
                }
            }
        };
    }
}
