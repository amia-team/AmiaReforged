using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Cqrs;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

/// <summary>
/// Central query dispatcher implementation.
/// Routes queries through the CQRS handler registry.
/// </summary>
[ServiceBinding(typeof(IQueryDispatcher))]
public sealed class QueryDispatcher : IQueryDispatcher
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly CqrsRegistry _registry;

    /// <summary>
    /// Production constructor used by Anvil DI.
    /// </summary>
    public QueryDispatcher(CqrsRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _registry = registry;
    }

    /// <summary>
    /// Creates a dispatcher with directly supplied handlers.
    /// Preserves the existing test/local-fixture construction path.
    /// </summary>
    internal QueryDispatcher(IEnumerable<IQueryHandlerMarker> queryHandlers)
    {
        ArgumentNullException.ThrowIfNull(queryHandlers);

        CqrsRegistry registry = new();

        foreach (IQueryHandlerMarker handler in queryHandlers)
        {
            registry.Register(handler);
        }

        _registry = registry;
    }

    /// <inheritdoc />
    public async Task<TResult> DispatchAsync<TQuery, TResult>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResult>
    {
        ArgumentNullException.ThrowIfNull(query);

        Type queryType = typeof(TQuery);
        string queryTypeName = queryType.Name;
        string resultTypeName = typeof(TResult).Name;

        Log.Debug(
            "Dispatching query: {QueryType} -> {ResultType}",
            queryTypeName,
            resultTypeName);

        try
        {
            if (!_registry.Queries.TryResolve<TQuery, TResult>(
                    out IQueryHandler<TQuery, TResult>? handler))
            {
                string errorMessage =
                    $"No handler registered for query type: {queryTypeName}";

                Log.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            TResult result =
                await handler.HandleAsync(query, cancellationToken)
                    .ConfigureAwait(false);

            Log.Debug(
                "Query {QueryType} executed successfully",
                queryTypeName);

            return result;
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "Error dispatching query: {QueryType}",
                queryTypeName);

            throw;
        }
    }
}
