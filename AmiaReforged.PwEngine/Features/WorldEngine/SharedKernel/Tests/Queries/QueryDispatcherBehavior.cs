using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Queries;

/// <summary>
/// Behavioral specifications for the QueryDispatcher.
/// Tests follow BDD-style naming: Context_Behavior_Outcome
/// </summary>
[TestFixture]
public class QueryDispatcherBehavior
{
    private TestQueryHandler _testHandler = null!;
    private QueryDispatcher _dispatcher = null!;

    [SetUp]
    public void SetUp()
    {
        _testHandler = new TestQueryHandler();

        // Create dispatcher with test handler
        List<IQueryHandlerMarker> handlers = new List<IQueryHandlerMarker> { _testHandler };
        _dispatcher = new QueryDispatcher(handlers);
    }

    // === Successful Query Execution ===

    [Test]
    public async Task GivenValidQuery_WhenDispatched_ThenHandlerIsInvokedAndResultReturned()
    {
        // Given: A valid test query
        TestQuery query = new TestQuery { SearchTerm = "test-data" };

        // When: The query is dispatched
        TestQueryResult result = await _dispatcher.DispatchAsync<TestQuery, TestQueryResult>(query);

        // Then: The handler was invoked and result returned
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.EqualTo("test-data-result"));
        Assert.That(_testHandler.LastHandledQuery, Is.EqualTo(query));
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(1));
    }

    [Test]
    public async Task GivenMultipleQueries_WhenDispatchedSequentially_ThenEachReturnsCorrectResult()
    {
        // Given: Three separate queries
        TestQuery query1 = new TestQuery { SearchTerm = "first" };
        TestQuery query2 = new TestQuery { SearchTerm = "second" };
        TestQuery query3 = new TestQuery { SearchTerm = "third" };

        // When: Each query is dispatched
        TestQueryResult result1 = await _dispatcher.DispatchAsync<TestQuery, TestQueryResult>(query1);
        TestQueryResult result2 = await _dispatcher.DispatchAsync<TestQuery, TestQueryResult>(query2);
        TestQueryResult result3 = await _dispatcher.DispatchAsync<TestQuery, TestQueryResult>(query3);

        // Then: Each result matches its query
        Assert.That(result1.Value, Is.EqualTo("first-result"));
        Assert.That(result2.Value, Is.EqualTo("second-result"));
        Assert.That(result3.Value, Is.EqualTo("third-result"));
        Assert.That(_testHandler.TimesHandled, Is.EqualTo(3));
    }

    [Test]
    public async Task GivenQueryReturningEmptyResult_WhenDispatched_ThenEmptyResultIsReturned()
    {
        // Given: A query that will return an empty result
        TestQuery query = new TestQuery { SearchTerm = "empty" };

        // When: The query is dispatched
        TestQueryResult result = await _dispatcher.DispatchAsync<TestQuery, TestQueryResult>(query);

        // Then: An empty result object is returned (not null)
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Value, Is.Empty);
    }

    // === Missing Handler ===

    [Test]
    public void GivenUnregisteredQuery_WhenDispatched_ThenInvalidOperationExceptionIsThrown()
    {
        // Given: A query with no registered handler
        UnhandledQuery query = new UnhandledQuery { Id = 42 };
        List<IQueryHandlerMarker> emptyHandlers = new List<IQueryHandlerMarker>();
        QueryDispatcher emptyDispatcher = new QueryDispatcher(emptyHandlers);

        // When/Then: Dispatching throws InvalidOperationException
        InvalidOperationException? exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await emptyDispatcher.DispatchAsync<UnhandledQuery, string>(query));

        Assert.That(exception!.Message, Does.Contain("No handler registered"));
        Assert.That(exception.Message, Does.Contain(nameof(UnhandledQuery)));
    }

    // === Handler Exceptions ===

    [Test]
    public void GivenHandlerThatThrows_WhenQueryDispatched_ThenExceptionIsPropagated()
    {
        // Given: A query that will cause the handler to throw
        TestQuery query = new TestQuery { SearchTerm = "throw", ShouldThrow = true };

        // When/Then: Exception is propagated to caller
        InvalidOperationException? exception = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _dispatcher.DispatchAsync<TestQuery, TestQueryResult>(query));

        Assert.That(exception!.Message, Is.EqualTo("Test exception"));
    }

    // === Cancellation ===

    [Test]
    public async Task GivenCancelledToken_WhenQueryIsDispatched_ThenCancellationIsRespected()
    {
        // Given: A query and a cancelled token
        TestQuery query = new TestQuery { SearchTerm = "cancellable" };
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        // When/Then: Query respects cancellation
        Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _dispatcher.DispatchAsync<TestQuery, TestQueryResult>(query, cts.Token));
    }

    // === Multiple Handler Types ===

    [Test]
    public async Task GivenDispatcherWithMultipleHandlerTypes_WhenDifferentQueriesDispatched_ThenCorrectHandlersAreInvoked()
    {
        // Given: A dispatcher with multiple handler types
        TestQueryHandler testHandler = new TestQueryHandler();
        AnotherQueryHandler anotherHandler = new AnotherQueryHandler();
        List<IQueryHandlerMarker> handlers = new List<IQueryHandlerMarker> { testHandler, anotherHandler };
        QueryDispatcher multiDispatcher = new QueryDispatcher(handlers);

        TestQuery testQuery = new TestQuery { SearchTerm = "test" };
        AnotherQuery anotherQuery = new AnotherQuery { Value = 123 };

        // When: Different queries are dispatched
        TestQueryResult testResult = await multiDispatcher.DispatchAsync<TestQuery, TestQueryResult>(testQuery);
        int anotherResult = await multiDispatcher.DispatchAsync<AnotherQuery, int>(anotherQuery);

        // Then: Each handler was invoked correctly
        Assert.That(testResult.Value, Is.EqualTo("test-result"));
        Assert.That(anotherResult, Is.EqualTo(246)); // 123 * 2
        Assert.That(testHandler.TimesHandled, Is.EqualTo(1));
        Assert.That(anotherHandler.TimesHandled, Is.EqualTo(1));
    }

    // === Test Fixtures ===

    private sealed class TestQuery : IQuery<TestQueryResult>
    {
        public string SearchTerm { get; set; } = string.Empty;
        public bool ShouldThrow { get; set; }
    }

    private sealed class TestQueryResult
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class UnhandledQuery : IQuery<string>
    {
        public int Id { get; set; }
    }

    private sealed class AnotherQuery : IQuery<int>
    {
        public int Value { get; set; }
    }

    private sealed class TestQueryHandler : IQueryHandler<TestQuery, TestQueryResult>
    {
        public TestQuery? LastHandledQuery { get; private set; }
        public int TimesHandled { get; private set; }

        public Task<TestQueryResult> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            if (query.ShouldThrow)
            {
                throw new InvalidOperationException("Test exception");
            }

            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }

            LastHandledQuery = query;
            TimesHandled++;

            TestQueryResult result = new TestQueryResult
            {
                Value = query.SearchTerm == "empty"
                    ? string.Empty
                    : (string.IsNullOrEmpty(query.SearchTerm) ? string.Empty : $"{query.SearchTerm}-result")
            };

            return Task.FromResult(result);
        }
    }

    private sealed class AnotherQueryHandler : IQueryHandler<AnotherQuery, int>
    {
        public int TimesHandled { get; private set; }

        public Task<int> HandleAsync(AnotherQuery query, CancellationToken cancellationToken = default)
        {
            TimesHandled++;
            return Task.FromResult(query.Value * 2);
        }
    }
}

