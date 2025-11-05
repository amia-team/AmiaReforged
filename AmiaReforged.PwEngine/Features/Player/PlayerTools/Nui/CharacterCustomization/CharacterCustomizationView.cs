using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WindowingSystem;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.PlayerTools.Nui.CharacterCustomization;

public sealed class CharacterCustomizationView : ScryView<CharacterCustomizationPresenter>, IToolWindow
{
    private const float WindowW = 700f;
    private const float WindowH = 800f;
    private const float HeaderW = 600f;
    private const float HeaderH = 100f;
    private const float HeaderTopPad = 8f;
    private const float HeaderLeftPad = 25f;

    public override CharacterCustomizationPresenter Presenter { get; protected set; }

    public readonly NuiBind<string> ModeName = new("cc_mode_name");
    public readonly NuiBind<string> PartName = new("cc_part_name");
    public readonly NuiBind<string> CurrentPartModelText = new("cc_part_model_text");
    public readonly NuiBind<int> CurrentPartModel = new("cc_part_model");
    public readonly NuiBind<int> CurrentColor = new("cc_current_color");
    public readonly NuiBind<bool> ArmorModeActive = new("cc_armor_active");
    public readonly NuiBind<bool> EquipmentModeActive = new("cc_equip_active");
    public readonly NuiBind<bool> AppearanceModeActive = new("cc_appear_active");
    public readonly NuiBind<bool> UseMetalPalette = new("cc_use_metal_palette");

    public readonly NuiBind<bool> ModelButtonsEnabled = new("cc_model_buttons_enabled");

    public readonly NuiBind<bool>[] ArmorPartVisible = new NuiBind<bool>[19];

    public readonly NuiBind<string>[] ColorResRef = new NuiBind<string>[176];

    public NuiButtonImage ArmorButton = null!;
    public NuiButtonImage EquipmentButton = null!;
    public NuiButtonImage AppearanceButton = null!;
    public NuiButtonImage PartLeftButton = null!;
    public NuiButtonImage PartRightButton = null!;
    public NuiButtonImage ModelLeft10Button = null!;
    public NuiButtonImage ModelLeftButton = null!;
    public NuiButtonImage ModelRightButton = null!;
    public NuiButtonImage ModelRight10Button = null!;
    public NuiButtonImage SaveButton = null!;
    public NuiButtonImage CancelButton = null!;
    public NuiButtonImage CloseButton = null!;
    public readonly NuiButtonImage ConfirmButton = null!;

    public NuiButton[] ArmorPartButtons = new NuiButton[19];

    public NuiButtonImage Cloth1Button = null!;
    public NuiButtonImage Cloth2Button = null!;
    public NuiButtonImage Leather1Button = null!;
    public NuiButtonImage Leather2Button = null!;
    public NuiButtonImage Metal1Button = null!;
    public NuiButtonImage Metal2Button = null!;

    public string Title => "Character Customization";
    public string Id => "char_customization";
    public string CategoryTag => "Appearance";
    public bool ListInPlayerTools => true;
    public bool RequiresPersistedCharacter => false;
    public IScryPresenter ForPlayer(NwPlayer player) => Presenter;

    public CharacterCustomizationView(NwPlayer player)
    {
        for (int i = 0; i < 19; i++)
        {
            ArmorPartVisible[i] = new NuiBind<bool>($"cc_armor_part_visible_{i}");
        }

        for (int i = 0; i < 176; i++)
        {
            ColorResRef[i] = new NuiBind<string>($"color_resref_{i}");
        }

        Presenter = new CharacterCustomizationPresenter(this, player);
    }

    private static NuiElement ImageButton(string id, string tooltip, out NuiButtonImage logicalButton,
        float width, float height, string resRef)
    {
        NuiButtonImage btn = new NuiButtonImage(resRef)
        {
            Id = id,
            Width = width,
            Height = height,
            Tooltip = tooltip
        }.Assign(out logicalButton);
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

    private NuiElement BuildModeButtons()
    {
        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 160f },
                        ImagePlatedLabeledButton("btn_mode_armor", "Armor", "Customize Equipped Armor", out ArmorButton, "app_armor_top", 100f, 100f),
                        new NuiSpacer { Width = 10f },
                        ImagePlatedLabeledButton("btn_mode_equipment", "Equipment", "Customize Other Equipment", out EquipmentButton, "app_misc", 100f, 100f),
                        new NuiSpacer { Width = 10f },
                        ImagePlatedLabeledButton("btn_mode_appearance", "Character", "Customize Character", out AppearanceButton, "cc_head_btn", 100f, 100f)
                    }
                }
            }
        };
    }

    private NuiElement BuildPartSelector()
    {
        return new NuiColumn
        {
            Visible = ArmorModeActive,
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 142.5f },
                        ImageButton("btn_part_left", "Previous Part", out PartLeftButton, 35f, 35f, "cc_arrow_l_btn"),
                        new NuiLabel(PartName)
                        {
                            Width = 300f,
                            Height = 35f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        ImageButton("btn_part_right", "Next Part", out PartRightButton, 35f, 35f, "cc_arrow_r_btn")
                    }
                }
            }
        };
    }

    private NuiElement BuildArmorBodyVisualization()
    {
        const float bodyWidth = 120f;
        const float bodyHeight = 120f;
        const float leftPad = 210f;

        List<NuiDrawListItem> drawList =
            [new NuiDrawListImage("app_armor_body", new NuiRect(leftPad, 0f, bodyWidth, bodyHeight))];

        int[] partImageMap = [16, 17, 14, 15, 12, 13, 7, 4, 10, 1, 6, 8, 3, 5, 0, 2, 9, 11, 18];

        for (int i = 0; i < 19; i++)
        {
            drawList.Add(new NuiDrawListImage($"ARMOR_PART_{partImageMap[i]}",
                new NuiRect(leftPad, 0f, bodyWidth, bodyHeight))
            {
                Enabled = ArmorPartVisible[i]
            });
        }

        return new NuiRow
        {
            Width = 0f,
            Height = bodyHeight,
            Children = new List<NuiElement>(),
            DrawList = drawList
        };
    }

    private NuiElement BuildModelSelector()
    {
        return new NuiColumn
        {
            Visible = ArmorModeActive,
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 145f },
                        new NuiLabel("Model:")
                        {
                            Width = 80f,
                            Height = 35f,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiButtonImage("cc_arrow_l_btn")
                        {
                            Id = "btn_model_left_10",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "-10",
                            Enabled = ModelButtonsEnabled
                        }.Assign(out ModelLeft10Button),
                        new NuiButtonImage("cc_arrow_l_btn")
                        {
                            Id = "btn_model_left",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "-1",
                            Enabled = ModelButtonsEnabled
                        }.Assign(out ModelLeftButton),
                        new NuiLabel(CurrentPartModelText)
                        {
                            Width = 60f,
                            Height = 35f,
                            HorizontalAlign = NuiHAlign.Center,
                            VerticalAlign = NuiVAlign.Middle,
                            ForegroundColor = new Color(30, 20, 12)
                        },
                        new NuiButtonImage("cc_arrow_r_btn")
                        {
                            Id = "btn_model_right",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "+1",
                            Enabled = ModelButtonsEnabled
                        }.Assign(out ModelRightButton),
                        new NuiButtonImage("cc_arrow_r_btn")
                        {
                            Id = "btn_model_right_10",
                            Width = 35f,
                            Height = 35f,
                            Tooltip = "+10",
                            Enabled = ModelButtonsEnabled
                        }.Assign(out ModelRight10Button)
                    }
                }
            }
        };
    }

    private NuiElement BuildMaterialSelector()
    {
        const float leftPad = 207.5f;

        return new NuiColumn
        {
            Children =
            {
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = leftPad },
                        ImageButton("btn_cloth1", "Cloth 1", out Cloth1Button, 35f, 35f, "ui_btn_c1"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_cloth2", "Cloth 2", out Cloth2Button, 35f, 35f, "ui_btn_c2"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_leather1", "Leather 1", out Leather1Button, 35f, 35f, "ui_btn_l1"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_leather2", "Leather 2", out Leather2Button, 35f, 35f, "ui_btn_l2"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_metal1", "Metal 1", out Metal1Button, 35f, 35f, "ui_btn_m1"),
                        new NuiSpacer { Width = 5f },
                        ImageButton("btn_metal2", "Metal 2", out Metal2Button, 35f, 35f, "ui_btn_m2")
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
            NuiRow rowElement = new NuiRow { Children =
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
                        ImagePlatedLabeledButton("btn_close", "", "Close window without saving", out CloseButton, "ui_btn_cancel"),
                    }
                }
            }
        };
    }

    private static NuiElement Divider(float thickness = 1f, byte alpha = 48)
    {
        return new NuiRow
        {
            Height = thickness + 4f,
            DrawList =
            [
                new NuiDrawListLine(new Color(0, 0, 0, alpha), false, thickness + 2f,
                    new NuiVector(0.0f, 100.0f), new NuiVector(0.0f, 400.0f))
            ]
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
                        new NuiSpacer { Width = 250f },
                        new NuiLabel("Character Customization")
                        {
                            Height = 20f,
                            ForegroundColor = new Color(30, 20, 12)
                        }
                    }
                },
                new NuiSpacer { Height = 10f },
                BuildModeButtons(),
                new NuiSpacer { Height = 10f },
                Divider(),
                new NuiSpacer { Height = 10f },
                BuildPartSelector(),
                new NuiSpacer { Height = 10f },
                BuildArmorBodyVisualization(),
                new NuiSpacer { Height = 270f },
                BuildModelSelector(),
                new NuiSpacer { Height = 20f },
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
                BuildMaterialSelector(),
                new NuiSpacer { Height = 5f },
                BuildColorPalette(),
                new NuiSpacer { Height = 10f },
                Divider(),
                new NuiSpacer { Height = 10f },
                BuildActionButtons()
            }
        };
    }
}

