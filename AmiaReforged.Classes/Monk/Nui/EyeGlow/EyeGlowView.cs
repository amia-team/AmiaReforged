using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Org.BouncyCastle.Asn1.Cmp;

namespace AmiaReforged.Classes.Monk.Nui.EyeGlow;

public sealed class EyeGlowView : ScryView<EyeGlowPresenter>
{
    public EyeGlowView(NwPlayer player)
    {
        Presenter = new EyeGlowPresenter(this, player);
    }

    public override EyeGlowPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiColumn root = new();

        root.Children.Add(new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = [],
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, 500f, 400f))]
        });

        foreach (EyeGlowType type in Enum.GetValues<EyeGlowType>())
        {
            string typeId = type.ToString();
            NuiBind<bool> rowVisibility = new($"visible_{typeId}");

            NuiRow itemRow = new()
            {
                Height = 35f,
                Children =
                {
                    new NuiButtonSelect(typeId, rowVisibility)
                    {
                        Id = $"select_{typeId}",
                        Width = 150f
                    },
                }
            };

            if (type != EyeGlowType.Remove)
            {
                itemRow.Children.Add(new NuiButton($"Confirm {typeId}")
                {
                    Id = $"confirm_{typeId}",
                    Visible = rowVisibility,
                    Width = 150f
                });
            }

            root.Children.Add(itemRow);
        }

        return root;
    }
}
