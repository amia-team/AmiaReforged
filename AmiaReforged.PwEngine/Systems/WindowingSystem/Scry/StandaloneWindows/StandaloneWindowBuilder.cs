using Anvil.API;

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
        return new SimplePopupBuilder();
    }
}

public class SimplePopupBuilder : ISimplePopupBuilder, IPlayerStage, ITitleStage, IMessageStage
{
    // N.B.: This is a special case where we build a fluent API on rails, don't instantiate the fields in the constructor
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private NwPlayer _nwPlayer;
    private string _title;
    private string _message;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


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

    public SimplePopupView Build()
    {
        return new SimplePopupView(_nwPlayer, _message, _title);
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
    SimplePopupView Build();
}