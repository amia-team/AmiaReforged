namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

/// <summary>
/// Central dispatcher for routing queries to their registered handlers.
/// Resolves handlers from DI and provides query execution infrastructure.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>
    /// Executes a query by resolving and invoking its handler.
    /// </summary>
    /// <typeparam name="TQuery">The query type implementing IQuery.</typeparam>
    /// <typeparam name="TResult">The result type returned by the query.</typeparam>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the query execution.</returns>
    Task<TResult> DispatchAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>;
}

