using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.SharedKernel;

/// <summary>
/// Tests for the CQRS (Command Query Responsibility Segregation) infrastructure.
/// Validates CommandResult, ICommand, and IQuery behavior.
/// </summary>
[TestFixture]
public class CqrsInfrastructureTests
{
    #region CommandResult Tests

    [Test]
    public void CommandResult_Ok_CreatesSuccessResult()
    {
        // Act
        CommandResult result = CommandResult.Ok();

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public void CommandResult_Ok_WithData_StoresData()
    {
        // Arrange
        Dictionary<string, object> data = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        CommandResult result = CommandResult.Ok(data);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["key1"], Is.EqualTo("value1"));
        Assert.That(result.Data["key2"], Is.EqualTo(42));
    }

    [Test]
    public void CommandResult_Fail_CreatesFailureResult()
    {
        // Arrange
        const string errorMessage = "Something went wrong";

        // Act
        CommandResult result = CommandResult.Fail(errorMessage);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
        Assert.That(result.Data, Is.Null);
    }

    [Test]
    public void CommandResult_OkWith_CreatesSingleDataResult()
    {
        // Act
        CommandResult result = CommandResult.OkWith("transactionId", 12345L);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["transactionId"], Is.EqualTo(12345L));
        Assert.That(result.Data.Count, Is.EqualTo(1));
    }

    [Test]
    public void CommandResult_IsRecord_SupportsCopyWith()
    {
        // Arrange
        CommandResult original = CommandResult.Ok();

        // Act
        CommandResult modified = original with { ErrorMessage = "Modified" };

        // Assert
        Assert.That(original.ErrorMessage, Is.Null); // Original unchanged
        Assert.That(modified.ErrorMessage, Is.EqualTo("Modified"));
    }

    #endregion

    #region Command Pattern Tests

    /// <summary>
    /// Test command for CQRS pattern validation
    /// </summary>
    private record TestCommand(string Name, int Value) : ICommand;

    /// <summary>
    /// Test command handler for CQRS pattern validation
    /// </summary>
    private class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public bool WasCalled { get; private set; }
        public TestCommand? LastCommand { get; private set; }

        public Task<CommandResult> HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastCommand = command;

            if (command.Value < 0)
            {
                return Task.FromResult(CommandResult.Fail("Value must be non-negative"));
            }

            return Task.FromResult(CommandResult.OkWith("result", command.Value * 2));
        }
    }

    [Test]
    public async Task CommandHandler_ExecutesCommand_ReturnsSuccess()
    {
        // Arrange
        TestCommandHandler handler = new TestCommandHandler();
        TestCommand command = new TestCommand("test", 5);

        // Act
        CommandResult result = await handler.HandleAsync(command);

        // Assert
        Assert.That(handler.WasCalled, Is.True);
        Assert.That(handler.LastCommand, Is.EqualTo(command));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!["result"], Is.EqualTo(10));
    }

    [Test]
    public async Task CommandHandler_WithInvalidInput_ReturnsFailure()
    {
        // Arrange
        TestCommandHandler handler = new TestCommandHandler();
        TestCommand command = new TestCommand("test", -5);

        // Act
        CommandResult result = await handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Value must be non-negative"));
    }

    [Test]
    public async Task CommandHandler_SupportsCancellation()
    {
        // Arrange
        TestCommandHandler handler = new TestCommandHandler();
        TestCommand command = new TestCommand("test", 5);
        CancellationTokenSource cts = new CancellationTokenSource();

        // Act - don't actually cancel, just verify it accepts the token
        CommandResult result = await handler.HandleAsync(command, cts.Token);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    #endregion

    #region Query Pattern Tests

    /// <summary>
    /// Test query for CQRS pattern validation
    /// </summary>
    private record TestQuery(string Filter) : IQuery<List<string>>;

    /// <summary>
    /// Test query handler for CQRS pattern validation
    /// </summary>
    private class TestQueryHandler : IQueryHandler<TestQuery, List<string>>
    {
        public bool WasCalled { get; private set; }
        public TestQuery? LastQuery { get; private set; }

        public Task<List<string>> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastQuery = query;

            List<string> results = new List<string> { "item1", "item2", "item3" };
            if (!string.IsNullOrEmpty(query.Filter))
            {
                results = results.Where(x => x.Contains(query.Filter)).ToList();
            }

            return Task.FromResult(results);
        }
    }

    [Test]
    public async Task QueryHandler_ExecutesQuery_ReturnsResults()
    {
        // Arrange
        TestQueryHandler handler = new TestQueryHandler();
        TestQuery query = new TestQuery("");

        // Act
        List<string> results = await handler.HandleAsync(query);

        // Assert
        Assert.That(handler.WasCalled, Is.True);
        Assert.That(handler.LastQuery, Is.EqualTo(query));
        Assert.That(results, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task QueryHandler_WithFilter_ReturnsFilteredResults()
    {
        // Arrange
        TestQueryHandler handler = new TestQueryHandler();
        TestQuery query = new TestQuery("1");

        // Act
        List<string> results = await handler.HandleAsync(query);

        // Assert
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0], Is.EqualTo("item1"));
    }

    [Test]
    public async Task QueryHandler_SupportsCancellation()
    {
        // Arrange
        TestQueryHandler handler = new TestQueryHandler();
        TestQuery query = new TestQuery("");
        CancellationTokenSource cts = new CancellationTokenSource();

        // Act - don't actually cancel, just verify it accepts the token
        List<string> results = await handler.HandleAsync(query, cts.Token);

        // Assert
        Assert.That(results, Is.Not.Null);
    }

    [Test]
    public async Task QueryHandler_DoesNotChangeState()
    {
        // Arrange
        TestQueryHandler handler = new TestQueryHandler();
        TestQuery query = new TestQuery("2");

        // Act - call multiple times
        List<string> results1 = await handler.HandleAsync(query);
        List<string> results2 = await handler.HandleAsync(query);

        // Assert - same results every time (idempotent)
        Assert.That(results1, Is.EqualTo(results2));
    }

    #endregion

    #region Pattern Enforcement Tests

    [Test]
    public void Commands_AreRecords_SupportValueEquality()
    {
        // Arrange
        TestCommand cmd1 = new TestCommand("test", 5);
        TestCommand cmd2 = new TestCommand("test", 5);
        TestCommand cmd3 = new TestCommand("test", 6);

        // Assert
        Assert.That(cmd1, Is.EqualTo(cmd2)); // Value equality
        Assert.That(cmd1, Is.Not.EqualTo(cmd3));
    }

    [Test]
    public void Queries_AreRecords_SupportValueEquality()
    {
        // Arrange
        TestQuery query1 = new TestQuery("filter1");
        TestQuery query2 = new TestQuery("filter1");
        TestQuery query3 = new TestQuery("filter2");

        // Assert
        Assert.That(query1, Is.EqualTo(query2)); // Value equality
        Assert.That(query1, Is.Not.EqualTo(query3));
    }

    [Test]
    public void Commands_AreImmutable()
    {
        // Arrange
        TestCommand cmd = new TestCommand("test", 5);

        // Act - create modified copy
        TestCommand modified = cmd with { Value = 10 };

        // Assert - original unchanged
        Assert.That(cmd.Value, Is.EqualTo(5));
        Assert.That(modified.Value, Is.EqualTo(10));
    }

    [Test]
    public void Queries_AreImmutable()
    {
        // Arrange
        TestQuery query = new TestQuery("original");

        // Act - create modified copy
        TestQuery modified = query with { Filter = "modified" };

        // Assert - original unchanged
        Assert.That(query.Filter, Is.EqualTo("original"));
        Assert.That(modified.Filter, Is.EqualTo("modified"));
    }

    #endregion
}

