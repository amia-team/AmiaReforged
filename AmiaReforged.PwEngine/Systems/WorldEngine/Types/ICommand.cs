namespace AmiaReforged.PwEngine.Systems.WorldEngine.Types;

public interface ICommand<out TResponse> {}

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command);
}
