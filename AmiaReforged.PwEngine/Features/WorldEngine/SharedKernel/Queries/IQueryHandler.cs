using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Cqrs;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

/// <summary>
/// Handler for executing queries.
/// Extends IQueryHandlerMarker for CQRS registry discovery.
/// </summary>
/// <typeparam name="TQuery">The query type to handle.</typeparam>
/// <typeparam name="TResult">The result type returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResult> : IQueryHandlerMarker
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Executes the query asynchronously.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Query result.</returns>
    Task<TResult> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Explicitly registers this handler instance with a CQRS registry.
    /// This is an escape hatch for manual composition and tests; production
    /// currently discovers DI registrations through CqrsRegistry.
    /// </summary>
    void Register(CqrsRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        registry.Queries.Register<TQuery, TResult>(this);
    }
}
