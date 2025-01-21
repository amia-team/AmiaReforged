using Anvil;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.StandaloneWindows;

public static class StandAloneWindow
{
    public static IWindowBuilder Builder()
    {
        return new StandaloneWindowBuilder();
    }
}

public class StandaloneWindowBuilder : IWindowBuilder, IWindowTypeStage
{
    public IWindowTypeStage For()
    {
        return this;
    }

    public ISimplePopupBuilder SimplePopup()
    {
        InjectionService? service = AnvilCore.GetService<InjectionService>();
        if (service == null)
        {
            LogManager.GetCurrentClassLogger().Error("InjectionService is not available");
            return new SimplePopupBuilder();
        }
        
        SimplePopupBuilder popupBuilder = service.Inject(new SimplePopupBuilder());
        return popupBuilder;
    }
}

public class SimplePopupBuilder : ISimplePopupBuilder, IPlayerStage, ITitleStage, IMessageStage, IOpenStage
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    // N.B.: This is a special case where we build a fluent API on rails, don't instantiate the fields in the constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private NwPlayer _nwPlayer;
    private string _title;
    private string _message;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    [Inject] private Lazy<WindowDirector>? Director { get; set; }


    public IPlayerStage WithPlayer(NwPlayer player)
    {
        _nwPlayer = player;
        return this;
    }

    public ITitleStage WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public IMessageStage WithMessage(string message)
    {
        _message = message;
        return this;
    }

    public IOpenStage Build()
    {
        return this;
    }

    public void Open()
    {
        if (Director == null)
        {
            Log.Error("WindowDirector is not injected");
            return;
        }
        Director.Value.OpenPopup(_nwPlayer, _title, _message);
    }
}

public interface IWindowBuilder
{
    IWindowTypeStage For();
}

public interface IWindowTypeStage
{
    ISimplePopupBuilder SimplePopup();
}

public interface ISimplePopupBuilder
{
    IPlayerStage WithPlayer(NwPlayer player);
}

public interface IPlayerStage
{
    ITitleStage WithTitle(string title);
}

public interface ITitleStage
{
    IMessageStage WithMessage(string message);
}

public interface IMessageStage
{
    IOpenStage Build();
}

public interface IOpenStage
{
    void Open();
}