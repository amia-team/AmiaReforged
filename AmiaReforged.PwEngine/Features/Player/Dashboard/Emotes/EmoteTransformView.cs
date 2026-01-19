using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Player.Dashboard.Emotes;

public sealed class EmoteTransformView : IScryView
{
    public NuiBind<float> TranslateX { get; } = new("translate_x");
    public NuiBind<float> TranslateY { get; } = new("translate_y");
    public NuiBind<float> TranslateZ { get; } = new("translate_z");

    public NuiLayout RootLayout()
    {
        NuiRow bgLayer = new()
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = new List<NuiDrawListItem> { new NuiDrawListImage("ui_bg_emote", new NuiRect(-25f, -25f, 300, 300)) }
        };
        NuiColumn root = new()
        {
            Children = new List<NuiElement>
            {
                bgLayer,
                // X Translation
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("X:")
                        {
                            Width = 30f,
                            Height = 25f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSliderFloat(TranslateX, -0.8f, 0.8f)
                        {
                            Width = 150f,
                            Height = 25f
                        },
                        new NuiSpacer { Width = 10f }
                    }
                },

                // Y Translation
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Y:")
                        {
                            Width = 30f,
                            Height = 25f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSliderFloat(TranslateY, -0.8f, 0.8f)
                        {
                            Width = 150f,
                            Height = 25f
                        },
                        new NuiSpacer { Width = 10f }
                    }
                },

                // Z Translation
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 10f },
                        new NuiLabel("Z:")
                        {
                            Width = 30f,
                            Height = 25f,
                            VerticalAlign = NuiVAlign.Middle
                        },
                        new NuiSliderFloat(TranslateZ, -0.5f, 0.8f)
                        {
                            Width = 150f,
                            Height = 25f
                        },
                        new NuiSpacer { Width = 10f }
                    }
                },

                new NuiSpacer { Height = 10f },

                // Reset button
                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer{ Width = 40f},
                        new NuiButton("Reset to Zero")
                        {
                            Id = "btn_reset",
                            Width = 120f,
                            Height = 30f
                        }
                    }
                }
            }
        };

        return root;
    }
}
