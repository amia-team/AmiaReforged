﻿using AmiaReforged.PwEngine.Features.WindowingSystem;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.CharacterTools.ThousandFaces;

public sealed class ThousandFacesView : ScryView<ThousandFacesPresenter>
{
    private const float WindowW = 700f;
    private const float WindowH = 800f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 25f;

    public override ThousandFacesPresenter Presenter { get; protected set; }

    // General control binds
    public readonly NuiBind<bool> AlwaysEnabled = new("tf_always_enabled");

    // Binds
    public readonly NuiBind<string> HeadModelText = new("tf_head_model");
    public readonly NuiBind<int> HeadModelInput = new("tf_head_model_input");

    public readonly NuiBind<string> AppearanceModelText = new("tf_appearance_model");
    public readonly NuiBind<int> AppearanceModelInput = new("tf_appearance_model_input");

    public readonly NuiBind<string> ScaleText = new("tf_scale");

    public readonly NuiBind<string> TempNameText = new("tf_temp_name");
    public readonly NuiBind<bool> TempNameConfirmEnabled = new("tf_temp_name_confirm_enabled");

    public readonly NuiBind<string> CurrentSoundsetText = new("tf_current_soundset");
    public readonly NuiBind<string> NewSoundsetText = new("tf_new_soundset");
    public readonly NuiBind<bool> SoundsetConfirmEnabled = new("tf_soundset_confirm_enabled");

    public readonly NuiBind<string> CurrentPortraitText = new("tf_current_portrait");
    public readonly NuiBind<string> NewPortraitText = new("tf_new_portrait");
    public readonly NuiBind<string> PortraitResRef = new("tf_portrait_resref");
    public readonly NuiBind<bool> PortraitConfirmEnabled = new("tf_portrait_confirm_enabled");

    public readonly NuiBind<int> CurrentColorChannel = new("tf_current_color_channel");
    public readonly NuiBind<string>[] SkinColorResRef = new NuiBind<string>[176];
    public readonly NuiBind<string>[] HairColorResRef = new NuiBind<string>[176];
    public readonly NuiBind<string>[] TattooColorResRef = new NuiBind<string>[176];

    // Button references
    public NuiButtonImage HeadModelLeft10Button = null!;
    public NuiButtonImage HeadModelLeftButton = null!;
    public NuiButtonImage HeadModelRightButton = null!;
    public NuiButtonImage HeadModelRight10Button = null!;
    public NuiButtonImage HeadModelSetButton = null!;
    public NuiTextEdit HeadModelInputField = null!;

    public NuiButtonImage AppearanceModelLeft10Button = null!;
    public NuiButtonImage AppearanceModelLeftButton = null!;
    public NuiButtonImage AppearanceModelRightButton = null!;
    public NuiButtonImage AppearanceModelRight10Button = null!;
    public NuiButtonImage AppearanceModelSetButton = null!;
    public NuiTextEdit AppearanceModelInputField = null!;
    public NuiButtonImage SwapGenderButton = null!;

    public NuiButtonImage ScaleMinButton = null!;
    public NuiButtonImage ScaleDecrease10Button = null!;
    public NuiButtonImage ScaleDecreaseButton = null!;
    public NuiButtonImage ScaleIncreaseButton = null!;
    public NuiButtonImage ScaleIncrease10Button = null!;
    public NuiButtonImage ScaleMaxButton = null!;

    public NuiTextEdit TempNameInputField = null!;
    public NuiButtonImage TempNameConfirmButton = null!;
    public NuiButtonImage RestoreNameButton = null!;

    public NuiTextEdit SoundsetInputField = null!;
    public NuiButtonImage SoundsetConfirmButton = null!;

    public NuiTextEdit PortraitInputField = null!;
    public NuiButtonImage PortraitConfirmButton = null!;

    public NuiButtonImage SkinButton = null!;
    public NuiButtonImage HairButton = null!;
    public NuiButtonImage Tattoo1Button = null!;
    public NuiButtonImage Tattoo2Button = null!;

    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage DiscardButton = null!;
    public NuiButtonImage CancelButton = null!;


    public ThousandFacesView(NwPlayer player, PlayerNameOverrideService playerNameOverrideService)
    {
        // Initialize skin color binds
        for (int i = 0; i < 176; i++)
        {
            SkinColorResRef[i] = new NuiBind<string>($"tf_skin_color_resref_{i}");
        }

        // Initialize hair color binds
        for (int i = 0; i < 176; i++)
        {
            HairColorResRef[i] = new NuiBind<string>($"tf_hair_color_resref_{i}");
        }

        // Initialize tattoo color binds (cloth/leather palette)
        for (int i = 0; i < 176; i++)
        {
            TattooColorResRef[i] = new NuiBind<string>($"tf_tattoo_color_resref_{i}");
        }

        Presenter = new ThousandFacesPresenter(this, player, playerNameOverrideService);
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
                BuildMainContent()
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

    private NuiElement BuildMainContent()
    {
        return new NuiColumn
        {
            Children =
            {
                // Title label
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 220f },
                        new NuiLabel("1000 Faces: Choose a Modification")
                        {
                            Height = 25f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },

                // Head section
                BuildHeadSection(),
                new NuiSpacer { Height = 10f },

                // Soundset section
                BuildSoundsetSection(),
                new NuiSpacer { Height = 10f },

                // Portrait section
                BuildPortraitSection(),
                new NuiSpacer { Height = 10f },

                // Appearance section
                BuildAppearanceSection(),
                new NuiSpacer { Height = 10f },

                // Temporary Name section
                BuildTempNameSection(),
                new NuiSpacer { Height = 10f },

                // Scale section
                BuildScaleSection(),
                new NuiSpacer { Height = 10f },

                // Color channel buttons
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 290f },
                        new NuiLabel("Change Colors")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                BuildColorChannelButtons(),
                new NuiSpacer { Height = 5f },

                // Color palette
                BuildColorPalette(),
                new NuiSpacer { Height = 10f },

                // Action buttons
                BuildActionButtons()
            }
        };
    }

    private NuiElement BuildHeadSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 325f },
                        new NuiLabel("Head")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 130f },
                        new NuiLabel("Head Model:")
                        {
                            Width = 120f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_head_left10", "-10", out HeadModelLeft10Button, 30f, 30f, "ui_btn_sm_min10"),
                        ImageButton("btn_head_left", "-1", out HeadModelLeftButton, 30f, 30f, "ui_btn_sm_min1"),
                        new NuiLabel(HeadModelText)
                        {
                            Width = 40f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_head_right", "+1", out HeadModelRightButton, 30f, 30f, "ui_btn_sm_plus1"),
                        ImageButton("btn_head_right10", "+10", out HeadModelRight10Button, 30f, 30f, "ui_btn_sm_plus10"),
                        new NuiTextEdit("", HeadModelText, 10, false)
                        {
                            Width = 60f,
                            Height = 30f
                        }.Assign(out HeadModelInputField),
                        ImageButton("btn_head_set", "Set head to specific number", out HeadModelSetButton, 30f, 30f, "ui_btn_sm_check")
                    }
                }
            }
        };
    }

    private NuiElement BuildSoundsetSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 315f },
                        new NuiLabel("Soundset")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiLabel("Current Soundset:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiLabel(CurrentSoundsetText)
                        {
                            Width = 400f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiLabel("New Soundset:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiTextEdit("Enter soundset number", NewSoundsetText, 50, false)
                        {
                            Width = 300f,
                            Height = 30f
                        }.Assign(out SoundsetInputField),
                        new NuiSpacer { Width = 10f },
                        ImageButton("btn_soundset_confirm", "Confirm soundset change", out SoundsetConfirmButton, 30f, 30f, "ui_btn_sm_check", SoundsetConfirmEnabled)
                    }
                }
            }
        };
    }

    private NuiElement BuildPortraitSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 320f },
                        new NuiLabel("Portrait")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 50f },
                        new NuiLabel("Current Portrait:")
                        {
                            Width = 150f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiLabel(CurrentPortraitText)
                        {
                            Width = 200f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 80f },
                        // Portrait preview image (medium size - 64x100)
                        new NuiImage(PortraitResRef)
                        {
                            Width = 64f,
                            Height = 100f,
                            HorizontalAlign = NuiHAlign.Left,
                            VerticalAlign = NuiVAlign.Middle,
                            ImageAspect = NuiAspect.Fit
                        },
                        new NuiSpacer { Width = 50f },
                        new NuiColumn
                        {
                            Children =
                            {
                                new NuiLabel("New Portrait:")
                                {
                                    Width = 150f,
                                    Height = 30f,
                                    VerticalAlign = NuiVAlign.Middle,
                                    ForegroundColor = new Color(30, 20, 12)
                                },
                                new NuiRow
                                {
                                    Children =
                                    {
                                        new NuiTextEdit("Enter portrait resref", NewPortraitText, 50, false)
                                        {
                                            Width = 300f,
                                            Height = 30f
                                        }.Assign(out PortraitInputField),
                                        new NuiSpacer { Width = 10f },
                                        ImageButton("btn_portrait_confirm", "Confirm portrait change", out PortraitConfirmButton, 30f, 30f, "ui_btn_sm_check", PortraitConfirmEnabled)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private NuiElement BuildAppearanceSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 305f },
                        new NuiLabel("Appearance")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 247.5f },
                        ImageButton("btn_appearance_left10", "-10", out AppearanceModelLeft10Button, 30f, 30f, "ui_btn_sm_min10"),
                        ImageButton("btn_appearance_left", "-1", out AppearanceModelLeftButton, 30f, 30f, "ui_btn_sm_min1"),
                        new NuiLabel(AppearanceModelText)
                        {
                            Width = 50f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_appearance_right", "+1", out AppearanceModelRightButton, 30f, 30f, "ui_btn_sm_plus1"),
                        ImageButton("btn_appearance_right10", "+10", out AppearanceModelRight10Button, 30f, 30f, "ui_btn_sm_plus10"),
                    }
                },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 312.5f },
                        new NuiTextEdit("", AppearanceModelText, 10, false)
                        {
                            Width = 60f,
                            Height = 30f
                        }.Assign(out AppearanceModelInputField),
                        ImageButton("btn_appearance_set", "Set appearance to specific number", out AppearanceModelSetButton, 30f, 30f, "ui_btn_sm_check")
                    }
                },
                new NuiSpacer { Height = 5f },
                // Base race quick revert buttons
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 210f },
                        new NuiButton("0") { Id = "btn_appearance_dwarf", Width = 30f, Height = 30f, Tooltip = "Dwarf" },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("1") { Id = "btn_appearance_elf", Width = 30f, Height = 30f, Tooltip = "Elf" },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("2") { Id = "btn_appearance_gnome", Width = 30f, Height = 30f, Tooltip = "Gnome" },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("3") { Id = "btn_appearance_halfling", Width = 30f, Height = 30f, Tooltip = "Halfling" },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("4") { Id = "btn_appearance_halfelf", Width = 30f, Height = 30f, Tooltip = "Half-Elf" },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("5") { Id = "btn_appearance_halforc", Width = 30f, Height = 30f, Tooltip = "Half-Orc" },
                        new NuiSpacer { Width = 5f },
                        new NuiButton("6") { Id = "btn_appearance_human", Width = 30f, Height = 30f, Tooltip = "Human" }
                    }
                },
                new NuiSpacer { Height = 5f },
                // Swap Gender button
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 270f },
                        new NuiButton("Swap Gender") { Id = "btn_swap_gender", Width = 150f, Height = 30f, Tooltip = "Swap Gender" }
                    }
                }
            }
        };
    }

    private NuiElement BuildTempNameSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 285f },
                        new NuiLabel("Temporary Name")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 190f },
                        new NuiTextEdit("Enter a temporary name...", TempNameText, 50, false)
                        {
                            Width = 300f,
                            Height = 30f,
                            Tooltip = "Enter a temporary name"
                        }.Assign(out TempNameInputField),
                        new NuiSpacer { Width = 10f },
                        ImageButton("btn_tempname_confirm", "Set temporary name", out TempNameConfirmButton, 30f, 30f, "ui_btn_sm_check", TempNameConfirmEnabled)
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 270f },
                        new NuiButton("Restore Name") { Id = "btn_restore_name", Width = 150f, Height = 30f, Tooltip = "Restore Original Name" }
                    }
                }
            }
        };
    }

    private NuiElement BuildScaleSection()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 300f },
                        new NuiLabel("Visual Scale")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 130f },
                        new NuiLabel("Scale:")
                        {
                            Width = 80f,
                            Height = 30f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_scale_min", "Set to minimum scale", out ScaleMinButton, 30f, 30f, "cc_arrow_l_btn"),
                        ImageButton("btn_scale_decrease10", "-10%", out ScaleDecrease10Button, 30f, 30f, "ui_btn_sm_min10"),
                        ImageButton("btn_scale_decrease", "-2%", out ScaleDecreaseButton, 30f, 30f, "ui_btn_sm_min"),
                        new NuiLabel(ScaleText)
                        {
                            Width = 50f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_scale_increase", "+2%", out ScaleIncreaseButton, 30f, 30f, "ui_btn_sm_plus"),
                        ImageButton("btn_scale_increase10", "+10%", out ScaleIncrease10Button, 30f, 30f, "ui_btn_sm_plus10"),
                        ImageButton("btn_scale_max", "Set to maximum scale", out ScaleMaxButton, 30f, 30f, "cc_arrow_r_btn")
                    }
                }
            }
        };
    }

    private NuiElement BuildColorChannelButtons()
    {
        const float leftPad = 252.5f;

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = leftPad },
                        ImageButton("btn_hair", "Hair", out HairButton, 35f, 35f, "ui_btn_hair"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_skin", "Skin", out SkinButton, 35f, 35f, "ui_btn_skin"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_tattoo1", "Tattoo 1", out Tattoo1Button, 35f, 35f, "ui_btn_tat1"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_tattoo2", "Tattoo 2", out Tattoo2Button, 35f, 35f, "ui_btn_tat2")
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

                // The resref will be updated dynamically based on which channel is selected
                NuiButtonImage colorBtn = new NuiButtonImage(SkinColorResRef[colorIndex])
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
                        ImagePlatedLabeledButton("btn_discard", "", "Discard changes and revert to last save", out DiscardButton, "ui_btn_discard"),
                        new NuiSpacer { Width = 20f },
                        ImagePlatedLabeledButton("btn_cancel", "", "Close window", out CancelButton, "ui_btn_cancel")
                    }
                }
            }
        };
    }

    private NuiElement ImageButton(string id, string tooltip, out NuiButtonImage button, float w, float h, string resRef, NuiBind<bool>? enabled = null)
    {
        NuiBind<bool> enabledBind = enabled ?? AlwaysEnabled;

        button = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = w,
            Height = h,
            Tooltip = tooltip,
            Enabled = enabledBind
        };
        return button;
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
                btn
            }
        };
    }
}

