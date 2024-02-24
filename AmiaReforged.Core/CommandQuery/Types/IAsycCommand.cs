using AmiaReforged.Core.CommandQuery.Spellbook;

namespace AmiaReforged.Core.CommandQuery.Types;

public interface IAsycCommand<in TContext, TResult> where TContext : ICommandContext where TResult : class
{
    public Task<IResult<TResult>> Execute(TContext context);
}