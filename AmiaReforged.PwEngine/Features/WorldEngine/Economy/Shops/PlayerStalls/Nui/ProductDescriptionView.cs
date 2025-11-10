using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;

public sealed class ProductDescriptionView : ScryView<ProductDescriptionPresenter>
{
    private const float WindowW = 480f;
    private const float WindowH = 420f;

    public readonly NuiBind<string> ProductDescription = new("product_full_description");

    public NuiButtonImage CloseButton = null!;

    public ProductDescriptionView(ProductDescriptionPresenter presenter)
    {
        Presenter = presenter;
    }

    public override ProductDescriptionPresenter Presenter { get; protected set; }

    public override NuiLayout RootLayout()
    {
        NuiRow bgLayer = new NuiRow
        {
            Width = 0f,
            Height = 0f,
            Children = new List<NuiElement>(),
            DrawList = [new NuiDrawListImage("ui_bg", new NuiRect(0f, 0f, WindowW, WindowH))]
        };

        CloseButton = new NuiButtonImage("ui_btn_cancel")
        {
            Id = "product_description_close",
            Width = 150f,
            Height = 38f
        };

        return new NuiColumn
        {
            Children =
            {
                bgLayer,
                new NuiSpacer { Height = 5f },
                new NuiRow
                {
                    Children =
                    {
                        new NuiLabel("Product Description")
                        {
                            Width = 460f,
                            Height = 30f,
                            HorizontalAlign = NuiHAlign.Center,
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
                        new NuiSpacer { Width = 20f },
                        new NuiText(ProductDescription)
                        {
                            Width = 420f,
                            Height = 250f
                        }
                    }
                },

                new NuiSpacer { Height = 10f },

                new NuiRow
                {
                    Children =
                    {
                        new NuiSpacer { Width = 150f },
                        CloseButton
                    }
                }
            }
        };
    }
}

public sealed class ProductDescriptionPresenter : ScryPresenter<ProductDescriptionView>
{
    private readonly NwPlayer _player;
    private readonly string _description;
    private NuiWindowToken _token;
    private NuiWindow? _window;

    public ProductDescriptionPresenter(NwPlayer player, string description)
    {
        _player = player;
        _description = description;
        View = new ProductDescriptionView(this);
    }

    public override ProductDescriptionView View { get; }

    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        _window = new NuiWindow(View.RootLayout(), "Product Description")
        {
            Geometry = new NuiRect(200f, 150f, 480f, 420f),
            Resizable = false
        };
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType == NuiEventType.Click && eventData.ElementId == View.CloseButton.Id)
        {
            Close();
        }
    }

    public override void Create()
    {
        if (_window is null)
        {
            InitBefore();
        }

        if (_window is null)
        {
            _player.SendServerMessage("Could not create product description window.", ColorConstants.Red);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Failed to open product description window.", ColorConstants.Red);
            return;
        }

        Token().SetBindValue(View.ProductDescription, _description);
        Token().OnNuiEvent += ProcessEvent;
    }

    public override void Close()
    {
        Token().OnNuiEvent -= ProcessEvent;
        _token.Close();
    }
}

