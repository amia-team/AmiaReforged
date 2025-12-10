﻿﻿using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class EquipmentCustomizationView : ScryView<EquipmentCustomizationPresenter>
{
    private const float WindowW = 700f;
    private const float WindowH = 800f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 25f;

    public override EquipmentCustomizationPresenter Presenter { get; protected set; }

    public readonly NuiBind<string> WeaponTopModelText = new("eq_weapon_top_model");
    public readonly NuiBind<string> WeaponMidModelText = new("eq_weapon_mid_model");
    public readonly NuiBind<string> WeaponBotModelText = new("eq_weapon_bot_model");
    public readonly NuiBind<string> WeaponScaleText = new("eq_weapon_scale");

    public readonly NuiBind<string> OffHandTopModelText = new("eq_offhand_top_model");
    public readonly NuiBind<string> OffHandMidModelText = new("eq_offhand_mid_model");
    public readonly NuiBind<string> OffHandBotModelText = new("eq_offhand_bot_model");
    public readonly NuiBind<string> OffHandScaleText = new("eq_offhand_scale");

    public readonly NuiBind<string> BootsTopModelText = new("eq_boots_top_model");
    public readonly NuiBind<string> BootsMidModelText = new("eq_boots_mid_model");
    public readonly NuiBind<string> BootsBotModelText = new("eq_boots_bot_model");

    public readonly NuiBind<string> HelmetAppearanceText = new("eq_helmet_appearance");
    public readonly NuiBind<string> CloakAppearanceText = new("eq_cloak_appearance");

    public readonly NuiBind<bool> WeaponSelected = new("eq_weapon_selected");
    public readonly NuiBind<bool> OffHandSelected = new("eq_offhand_selected");
    public readonly NuiBind<bool> BootsSelected = new("eq_boots_selected");
    public readonly NuiBind<bool> HelmetSelected = new("eq_helmet_selected");
    public readonly NuiBind<bool> CloakSelected = new("eq_cloak_selected");

    public readonly NuiBind<bool> WeaponControlsEnabled = new("eq_weapon_controls_enabled");
    public readonly NuiBind<bool> WeaponMidBotEnabled = new("eq_weapon_midbot_enabled");
    public readonly NuiBind<bool> OffHandControlsEnabled = new("eq_offhand_controls_enabled");
    public readonly NuiBind<bool> OffHandMidBotEnabled = new("eq_offhand_midbot_enabled");
    public readonly NuiBind<bool> BootsControlsEnabled = new("eq_boots_controls_enabled");
    public readonly NuiBind<bool> HelmetControlsEnabled = new("eq_helmet_controls_enabled");
    public readonly NuiBind<bool> CloakControlsEnabled = new("eq_cloak_controls_enabled");
    public readonly NuiBind<bool> ChannelButtonsEnabled = new("eq_channel_buttons_enabled");
    public readonly NuiBind<bool> AlwaysEnabled = new("eq_always_enabled");

    public readonly NuiBind<string>[] ColorResRef = new NuiBind<string>[176];

    public NuiButtonImage WeaponButton = null!;
    public NuiButtonImage OffHandButton = null!;
    public NuiButtonImage BootsButton = null!;
    public NuiButtonImage HelmetButton = null!;
    public NuiButtonImage CloakButton = null!;

    public NuiButtonImage WeaponCopyButton = null!;
    public NuiButtonImage OffHandCopyButton = null!;
    public NuiButtonImage BootsCopyButton = null!;
    public NuiButtonImage HelmetCopyButton = null!;
    public NuiButtonImage CloakCopyButton = null!;

    public NuiButtonImage WeaponTopModelLeftButton = null!;
    public NuiButtonImage WeaponTopModelRightButton = null!;
    public NuiButtonImage WeaponMidModelLeftButton = null!;
    public NuiButtonImage WeaponMidModelRightButton = null!;
    public NuiButtonImage WeaponBotModelLeftButton = null!;
    public NuiButtonImage WeaponBotModelRightButton = null!;

    public NuiButtonImage WeaponTopModelLeft10Button = null!;
    public NuiButtonImage WeaponTopModelRight10Button = null!;
    public NuiButtonImage WeaponMidModelLeft10Button = null!;
    public NuiButtonImage WeaponMidModelRight10Button = null!;
    public NuiButtonImage WeaponBotModelLeft10Button = null!;
    public NuiButtonImage WeaponBotModelRight10Button = null!;

    public NuiButtonImage WeaponScaleMinusButton = null!;
    public NuiButtonImage WeaponScalePlusButton = null!;

    public NuiButtonImage OffHandTopModelLeftButton = null!;
    public NuiButtonImage OffHandTopModelRightButton = null!;
    public NuiButtonImage OffHandMidModelLeftButton = null!;
    public NuiButtonImage OffHandMidModelRightButton = null!;
    public NuiButtonImage OffHandBotModelLeftButton = null!;
    public NuiButtonImage OffHandBotModelRightButton = null!;

    public NuiButtonImage OffHandTopModelLeft10Button = null!;
    public NuiButtonImage OffHandTopModelRight10Button = null!;
    public NuiButtonImage OffHandMidModelLeft10Button = null!;
    public NuiButtonImage OffHandMidModelRight10Button = null!;
    public NuiButtonImage OffHandBotModelLeft10Button = null!;
    public NuiButtonImage OffHandBotModelRight10Button = null!;

    public NuiButtonImage OffHandScaleMinusButton = null!;
    public NuiButtonImage OffHandScalePlusButton = null!;

    public NuiButtonImage BootsTopModelLeftButton = null!;
    public NuiButtonImage BootsTopModelRightButton = null!;
    public NuiButtonImage BootsMidModelLeftButton = null!;
    public NuiButtonImage BootsMidModelRightButton = null!;
    public NuiButtonImage BootsBotModelLeftButton = null!;
    public NuiButtonImage BootsBotModelRightButton = null!;


    public NuiButtonImage HelmetAppearanceLeftButton = null!;
    public NuiButtonImage HelmetAppearanceRightButton = null!;
    public NuiButtonImage CloakAppearanceLeftButton = null!;
    public NuiButtonImage CloakAppearanceRightButton = null!;

    public NuiButtonImage Cloth1Button = null!;
    public NuiButtonImage Cloth2Button = null!;
    public NuiButtonImage Leather1Button = null!;
    public NuiButtonImage Leather2Button = null!;
    public NuiButtonImage Metal1Button = null!;
    public NuiButtonImage Metal2Button = null!;

    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage CancelButton = null!;
    public NuiButtonImage CloseButton = null!;

    public string Title => "Customize Equipment";

    public EquipmentCustomizationView(NwPlayer player)
    {
        for (int i = 0; i < 176; i++)
        {
            ColorResRef[i] = new NuiBind<string>($"eq_color_resref_{i}");
        }

        Presenter = new EquipmentCustomizationPresenter(this, player);
    }

    private static NuiElement ImageButton(string id, string tooltip, out NuiButtonImage logicalButton,
        float width, float height, string resRef, NuiBind<bool>? enabled = null)
    {
        // If no enabled bind is provided, button will use the bind from the calling context
        // For equipment type buttons, we want them always enabled
        NuiButtonImage btn = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);

        if (enabled != null)
        {
            btn.Enabled = enabled;
        }

        return btn;
    }

    private static NuiElement ImagePlatedLabeledButton(string id, string label, string tooltip, out NuiButtonImage logicalButton,
        string resRef, float width = 150f, float height = 38f)
    {
        NuiButtonImage btn = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Width = width,
                    Children =
                    {
                        btn
                    }
                },
                new NuiRow
                {
                    Width = width,
                    Children =
                    {
                        new NuiLabel(label)
                        {
                            Width = width,
                            Height = 18f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                }
            }
        };
    }

    private NuiElement BuildHeaderOverlay()
    {
        return new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_header", new NuiRect(HeaderLeftPad, HeaderTopPad, HeaderW, HeaderH))]
        };
    }

    private NuiElement BuildWeaponSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 307.5f },
                        ImageButton("btn_weapon", "Customize Main Hand", out WeaponButton, 50f, 50f, "app_sword"),
                        new NuiSpacer { Width = 5f},
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiSpacer { Height = 10f },
                                ImageButton("btn_weapon_copy", "Copy Appearance", out WeaponCopyButton, 30f, 30f, "app_copy")
                            }
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        new NuiLabel("Main Hand")
                        {
                            Width = 90f,
                            Height = 18f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 142f },
                        new NuiLabel("Top Model:")
                        {
                            Width = 100f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_weapon_top_model_left10", "-10", out WeaponTopModelLeft10Button, 30f, 30f, "cc_arrow_l_btn", WeaponControlsEnabled),
                        ImageButton("btn_weapon_top_model_left", "-1", out WeaponTopModelLeftButton, 30f, 30f, "cc_arrow_l_btn", WeaponControlsEnabled),
                        new NuiLabel(WeaponTopModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_weapon_top_model_right", "+1", out WeaponTopModelRightButton, 30f, 30f, "cc_arrow_r_btn", WeaponControlsEnabled),
                        ImageButton("btn_weapon_top_model_right10", "+10", out WeaponTopModelRight10Button, 30f, 30f, "cc_arrow_r_btn", WeaponControlsEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 122f },
                        new NuiLabel("Middle Model:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_weapon_mid_model_left10", "-10", out WeaponMidModelLeft10Button, 30f, 30f, "cc_arrow_l_btn", WeaponMidBotEnabled),
                        ImageButton("btn_weapon_mid_model_left", "-1", out WeaponMidModelLeftButton, 30f, 30f, "cc_arrow_l_btn", WeaponMidBotEnabled),
                        new NuiLabel(WeaponMidModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_weapon_mid_model_right", "+1", out WeaponMidModelRightButton, 30f, 30f, "cc_arrow_r_btn", WeaponMidBotEnabled),
                        ImageButton("btn_weapon_mid_model_right10", "+10", out WeaponMidModelRight10Button, 30f, 30f, "cc_arrow_r_btn", WeaponMidBotEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 120f },
                        new NuiLabel("Bottom Model:")
                        {
                            Width = 122f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_weapon_bot_model_left10", "-10", out WeaponBotModelLeft10Button, 30f, 30f, "cc_arrow_l_btn", WeaponMidBotEnabled),
                        ImageButton("btn_weapon_bot_model_left", "-1", out WeaponBotModelLeftButton, 30f, 30f, "cc_arrow_l_btn", WeaponMidBotEnabled),
                        new NuiLabel(WeaponBotModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_weapon_bot_model_right", "+1", out WeaponBotModelRightButton, 30f, 30f, "cc_arrow_r_btn", WeaponMidBotEnabled),
                        ImageButton("btn_weapon_bot_model_right10", "+10", out WeaponBotModelRight10Button, 30f, 30f, "cc_arrow_r_btn", WeaponMidBotEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 117f },
                        new NuiLabel("Main Hand Scale:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 40f },
                        ImageButton("btn_weapon_scale_minus", "-5%", out WeaponScaleMinusButton, 30f, 30f, "ui_btn_sm_min", WeaponControlsEnabled),
                        new NuiLabel(WeaponScaleText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_weapon_scale_plus", "+5%", out WeaponScalePlusButton, 30f, 30f, "ui_btn_sm_plus", WeaponControlsEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildOffHandSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 307.5f },
                        ImageButton("btn_offhand", "Customize Off-Hand", out OffHandButton, 50f, 50f, "app_sword"),
                        new NuiSpacer { Width = 5f},
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiSpacer { Height = 10f },
                                ImageButton("btn_offhand_copy", "Copy Appearance", out OffHandCopyButton, 30f, 30f, "app_copy")
                            }
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        new NuiLabel("Off-Hand")
                        {
                            Width = 90f,
                            Height = 18f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 142f },
                        new NuiLabel("Top Model:")
                        {
                            Width = 100f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_offhand_top_model_left10", "-10", out OffHandTopModelLeft10Button, 30f, 30f, "cc_arrow_l_btn", OffHandControlsEnabled),
                        ImageButton("btn_offhand_top_model_left", "-1", out OffHandTopModelLeftButton, 30f, 30f, "cc_arrow_l_btn", OffHandControlsEnabled),
                        new NuiLabel(OffHandTopModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_offhand_top_model_right", "+1", out OffHandTopModelRightButton, 30f, 30f, "cc_arrow_r_btn", OffHandControlsEnabled),
                        ImageButton("btn_offhand_top_model_right10", "+10", out OffHandTopModelRight10Button, 30f, 30f, "cc_arrow_r_btn", OffHandControlsEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 122f },
                        new NuiLabel("Middle Model:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_offhand_mid_model_left10", "-10", out OffHandMidModelLeft10Button, 30f, 30f, "cc_arrow_l_btn", OffHandMidBotEnabled),
                        ImageButton("btn_offhand_mid_model_left", "-1", out OffHandMidModelLeftButton, 30f, 30f, "cc_arrow_l_btn", OffHandMidBotEnabled),
                        new NuiLabel(OffHandMidModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_offhand_mid_model_right", "+1", out OffHandMidModelRightButton, 30f, 30f, "cc_arrow_r_btn", OffHandMidBotEnabled),
                        ImageButton("btn_offhand_mid_model_right10", "+10", out OffHandMidModelRight10Button, 30f, 30f, "cc_arrow_r_btn", OffHandMidBotEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 120f },
                        new NuiLabel("Bottom Model:")
                        {
                            Width = 122f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_offhand_bot_model_left10", "-10", out OffHandBotModelLeft10Button, 30f, 30f, "cc_arrow_l_btn", OffHandMidBotEnabled),
                        ImageButton("btn_offhand_bot_model_left", "-1", out OffHandBotModelLeftButton, 30f, 30f, "cc_arrow_l_btn", OffHandMidBotEnabled),
                        new NuiLabel(OffHandBotModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_offhand_bot_model_right", "+1", out OffHandBotModelRightButton, 30f, 30f, "cc_arrow_r_btn", OffHandMidBotEnabled),
                        ImageButton("btn_offhand_bot_model_right10", "+10", out OffHandBotModelRight10Button, 30f, 30f, "cc_arrow_r_btn", OffHandMidBotEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 117f },
                        new NuiLabel("Off-Hand Scale:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiSpacer { Width = 40f },
                        ImageButton("btn_offhand_scale_minus", "-5%", out OffHandScaleMinusButton, 30f, 30f, "ui_btn_sm_min", OffHandControlsEnabled),
                        new NuiLabel(OffHandScaleText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_offhand_scale_plus", "+5%", out OffHandScalePlusButton, 30f, 30f, "ui_btn_sm_plus", OffHandControlsEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildBootsSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiSpacer { Height = 10f },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 307.5f },
                        ImageButton("btn_boots", "Customize Boots", out BootsButton, 50f, 50f, "app_boot"),
                        new NuiSpacer { Width = 5f },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiSpacer { Height = 10f },
                                ImageButton("btn_boots_copy", "Copy Appearance", out BootsCopyButton, 30f, 30f, "app_copy")
                            }
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        new NuiLabel("Boots")
                        {
                            Width = 90f,
                            Height = 18f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 176f },
                        new NuiLabel("Top Model:")
                        {
                            Width = 100f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_boots_top_model_left", "-1", out BootsTopModelLeftButton, 30f, 30f, "cc_arrow_l_btn", BootsControlsEnabled),
                        new NuiLabel(BootsTopModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_boots_top_model_right", "+1", out BootsTopModelRightButton, 30f, 30f, "cc_arrow_r_btn", BootsControlsEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 156f },
                        new NuiLabel("Middle Model:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_boots_mid_model_left", "-1", out BootsMidModelLeftButton, 30f, 30f, "cc_arrow_l_btn", BootsControlsEnabled),
                        new NuiLabel(BootsMidModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_boots_mid_model_right", "+1", out BootsMidModelRightButton, 30f, 30f, "cc_arrow_r_btn", BootsControlsEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 154f },
                        new NuiLabel("Bottom Model:")
                        {
                            Width = 122f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_boots_bot_model_left", "-1", out BootsBotModelLeftButton, 30f, 30f, "cc_arrow_l_btn", BootsControlsEnabled),
                        new NuiLabel(BootsBotModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_boots_bot_model_right", "+1", out BootsBotModelRightButton, 30f, 30f, "cc_arrow_r_btn", BootsControlsEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildHelmetCloakSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiSpacer { Height = 10f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 307.5f },
                        ImageButton("btn_helmet", "Customize Helmet", out HelmetButton, 50f, 50f, "app_helmet"),
                        new NuiSpacer { Width = 5f },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiSpacer { Height = 10f },
                                ImageButton("btn_helmet_copy", "Copy Appearance", out HelmetCopyButton, 30f, 30f, "app_copy")
                            }
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        new NuiLabel("Helmet")
                        {
                            Width = 90f,
                            Height = 18f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 126f },
                        new NuiLabel("Helmet Appearance:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_helmet_appearance_left", "-1", out HelmetAppearanceLeftButton, 30f, 30f, "cc_arrow_l_btn", HelmetControlsEnabled),
                        new NuiLabel(HelmetAppearanceText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_helmet_appearance_right", "+1", out HelmetAppearanceRightButton, 30f, 30f, "cc_arrow_r_btn", HelmetControlsEnabled)
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 307.5f },
                        ImageButton("btn_cloak", "Customize Cloak", out CloakButton, 50f, 50f, "app_cloak"),
                        new NuiSpacer { Width = 5f },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiSpacer { Height = 10f },
                                ImageButton("btn_cloak_copy", "Copy Appearance", out CloakCopyButton, 30f, 30f, "app_copy")
                            }
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        new NuiLabel("Cloak")
                        {
                            Width = 90f,
                            Height = 18f,
                            HorizontalAlign = NuiHAlign.Center,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 136f },
                        new NuiLabel("Cloak Appearance:")
                        {
                            Width = 140f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_cloak_appearance_left", "-1", out CloakAppearanceLeftButton, 30f, 30f, "cc_arrow_l_btn", CloakControlsEnabled),
                        new NuiLabel(CloakAppearanceText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_cloak_appearance_right", "+1", out CloakAppearanceRightButton, 30f, 30f, "cc_arrow_r_btn", CloakControlsEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildColorPalette()
    {
        List<NuiElement> colorGrid = new List<NuiElement>();

        for (int row = 0; row < 11; row++)
        {
            NuiRow rowElement = new NuiRow
            {
                Children =
                [
                    new NuiSpacer { Width = 65f }
                ]
            };

            for (int col = 0; col < 16; col++)
            {
                int colorIndex = row * 16 + col;
                if (colorIndex >= 176) break;

                NuiButtonImage colorBtn = new NuiButtonImage(ColorResRef[colorIndex])
                {
                    Id = $"btn_color_{colorIndex}",
                    Width = 30f,
                    Height = 30f,
                    Tooltip = $"Color {colorIndex}"
                };

                rowElement.Children.Add(colorBtn);
            }

            colorGrid.Add(rowElement);
        }

        return new NuiColumn
        {
            Children = colorGrid
        };
    }

    private NuiElement BuildChannelButtons()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 207.5f },
                        ImageButton("btn_cloth1", "Cloth 1", out Cloth1Button, 35f, 35f, "ui_btn_c1", ChannelButtonsEnabled),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_cloth2", "Cloth 2", out Cloth2Button, 35f, 35f, "ui_btn_c2", ChannelButtonsEnabled),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_leather1", "Leather 1", out Leather1Button, 35f, 35f, "ui_btn_l1", ChannelButtonsEnabled),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_leather2", "Leather 2", out Leather2Button, 35f, 35f, "ui_btn_l2", ChannelButtonsEnabled),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_metal1", "Metal 1", out Metal1Button, 35f, 35f, "ui_btn_m1", ChannelButtonsEnabled),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_metal2", "Metal 2", out Metal2Button, 35f, 35f, "ui_btn_m2", ChannelButtonsEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildActionButtons()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 80f },
                        ImagePlatedLabeledButton("btn_save", "", "Save Changes", out SaveButton, "ui_btn_save"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_cancel", "", "Discard changes and revert to last save", out CancelButton, "ui_btn_discard"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_close", "", "Close window", out CloseButton, "ui_btn_cancel")
                    }
                }
            }
        };
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

        NuiElement headerOverlay = BuildHeaderOverlay();
        NuiSpacer headerSpacer = new NuiSpacer { Height = HeaderH + HeaderTopPad + 6f };

        return new NuiColumn
        {
            Width = WindowW,
            Height = WindowH,
            Children =
            {
                bgLayer,
                headerOverlay,
                headerSpacer,
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 180f },
                        new NuiLabel("Select an equipment type to customize.")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                BuildWeaponSection(),
                BuildOffHandSection(),
                BuildBootsSection(),
                BuildHelmetCloakSection(),
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        new NuiLabel("Color Palette")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                BuildChannelButtons(),
                new NuiSpacer { Height = 5f },
                BuildColorPalette(),
                new NuiSpacer { Height = 5f },
                BuildActionButtons()
            }
        };
    }
}

