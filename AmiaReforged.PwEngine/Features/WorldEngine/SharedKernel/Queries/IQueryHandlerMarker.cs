namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

/// <summary>
/// Non-generic marker for query-handler discovery.
/// IQueryHandler&lt;TQuery, TResult&gt; implementations inherit this marker so
/// the CQRS registry can identify handler implementation types without requiring
/// every handler to be separately registered under the marker service type.
/// </summary>
public interface IQueryHandlerMarker
{
}
