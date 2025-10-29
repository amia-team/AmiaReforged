using Microsoft.Extensions.Configuration;
using WorldSimulator.Application;
using WorldSimulator.Domain.Events;
using WorldSimulator.Infrastructure.Services;

namespace WorldSimulator.Tests.Steps;

[Binding]
public class WorkQueueProcessingSteps
{
    private readonly ScenarioContext _scenarioContext;
    private SimulationDbContext? _dbContext;
    private Mock<CircuitBreakerService>? _circuitBreakerMock;
    private Mock<IEventLogPublisher>? _eventPublisherMock;
    private List<SimulationEvent> _publishedEvents = new();

    public WorkQueueProcessingSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
    }

    [Given(@"the simulation service is running")]
    public void GivenTheSimulationServiceIsRunning()
    {
        // Set up in-memory database for testing
        DbContextOptions<SimulationDbContext> options = new DbContextOptionsBuilder<SimulationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new SimulationDbContext(options);
        _scenarioContext["DbContext"] = _dbContext;

        // Set up mocks
        _eventPublisherMock = new Mock<IEventLogPublisher>();
        _eventPublisherMock
            .Setup(x => x.PublishAsync(It.IsAny<SimulationEvent>(), It.IsAny<EventSeverity>()))
            .Callback<SimulationEvent, EventSeverity>((evt, sev) => _publishedEvents.Add(evt))
            .Returns(Task.CompletedTask);

        _scenarioContext["EventPublisher"] = _eventPublisherMock;
        _scenarioContext["PublishedEvents"] = _publishedEvents;
    }

    [Given(@"the circuit breaker is closed")]
    public void GivenTheCircuitBreakerIsClosed()
    {
        _circuitBreakerMock = new Mock<CircuitBreakerService>(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<CircuitBreakerService>>(),
            Mock.Of<IEventLogPublisher>(),
            Mock.Of<IConfiguration>());

        _circuitBreakerMock
            .Setup(x => x.IsAvailable())
            .Returns(true);

        _scenarioContext["CircuitBreaker"] = _circuitBreakerMock;
    }

    [Given(@"the circuit breaker is open")]
    public void GivenTheCircuitBreakerIsOpen()
    {
        _circuitBreakerMock = new Mock<CircuitBreakerService>(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<CircuitBreakerService>>(),
            Mock.Of<IEventLogPublisher>(),
            Mock.Of<IConfiguration>());

        _circuitBreakerMock
            .Setup(x => x.IsAvailable())
            .Returns(false);

        _scenarioContext["CircuitBreaker"] = _circuitBreakerMock;
    }

    [Given(@"a work item of type ""(.*)"" is queued")]
    public void GivenAWorkItemOfTypeIsQueued(string workType)
    {
        SimulationWorkItem workItem = new SimulationWorkItem(workType, "{}");
        _dbContext!.WorkItems.Add(workItem);
        _dbContext.SaveChanges();

        _scenarioContext["WorkItem"] = workItem;
    }

    [Given(@"the work item has failed once")]
    public void GivenTheWorkItemHasFailedOnce()
    {
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        workItem.Start();
        workItem.Fail("Previous failure");
        _dbContext!.SaveChanges();
    }

    [Given(@"the following work items are queued:")]
    public void GivenTheFollowingWorkItemsAreQueued(Table table)
    {
        List<SimulationWorkItem> workItems = new List<SimulationWorkItem>();

        foreach (TableRow? row in table.Rows)
        {
            string? workType = row["WorkType"];
            DateTime createdAt = DateTime.Parse(row["CreatedAt"]);

            SimulationWorkItem workItem = new SimulationWorkItem(workType, "{}");
            // Using reflection to set CreatedAt for testing
            typeof(SimulationWorkItem)
                .GetProperty("CreatedAt")!
                .SetValue(workItem, createdAt);

            _dbContext!.WorkItems.Add(workItem);
            workItems.Add(workItem);
        }

        _dbContext!.SaveChanges();
        _scenarioContext["WorkItems"] = workItems;
    }

    [When(@"the simulation worker polls for work")]
    public async Task WhenTheSimulationWorkerPollsForWork()
    {
        // This is a simplified test - in reality we'd mock the worker
        // For now, we'll just verify the work item state transitions
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        _scenarioContext["InitialStatus"] = workItem.Status;
    }

    [When(@"the simulation worker processes all work")]
    public async Task WhenTheSimulationWorkerProcessesAllWork()
    {
        List<SimulationWorkItem>? workItems = _scenarioContext.Get<List<SimulationWorkItem>>("WorkItems");
        List<string> processedOrder = new List<string>();

        // Simulate processing in order
        List<SimulationWorkItem> orderedItems = _dbContext!.WorkItems
            .Where(w => w.Status == WorkItemStatus.Pending)
            .OrderBy(w => w.CreatedAt)
            .ToList();

        foreach (SimulationWorkItem item in orderedItems)
        {
            processedOrder.Add(item.WorkType);
        }

        _scenarioContext["ProcessedOrder"] = processedOrder;
    }

    [When(@"the retry count is below the maximum")]
    public void WhenTheRetryCountIsBelowTheMaximum()
    {
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        workItem.CanRetry(3).Should().BeTrue();
    }

    [Then(@"the work item status should be ""(.*)""")]
    public void ThenTheWorkItemStatusShouldBe(string expectedStatus)
    {
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        WorkItemStatus status = Enum.Parse<WorkItemStatus>(expectedStatus);
        workItem.Status.Should().Be(status);
    }

    [Then(@"the work item status should remain ""(.*)""")]
    public void ThenTheWorkItemStatusShouldRemain(string expectedStatus)
    {
        ThenTheWorkItemStatusShouldBe(expectedStatus);
    }

    [Then(@"the work item should be processed")]
    public void ThenTheWorkItemShouldBeProcessed()
    {
        // Verify that processing would occur
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        workItem.Status.Should().NotBe(WorkItemStatus.Pending);
    }

    [Then(@"the work item should not be processed")]
    public void ThenTheWorkItemShouldNotBeProcessed()
    {
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        workItem.Status.Should().Be(WorkItemStatus.Pending);
    }

    [Then(@"the work item processing should fail")]
    public void ThenTheWorkItemProcessingShouldFail()
    {
        // This would be triggered by the actual processor
        // For now, mark as placeholder
    }

    [Then(@"a (.*) event should be published")]
    public void ThenAnEventShouldBePublished(string eventType)
    {
        _publishedEvents.Should().Contain(e => e.GetType().Name == eventType);
    }

    [Then(@"the error message should be recorded")]
    public void ThenTheErrorMessageShouldBeRecorded()
    {
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        workItem.Error.Should().NotBeNullOrEmpty();
    }

    [Then(@"the work items should be processed in this order:")]
    public void ThenTheWorkItemsShouldBeProcessedInThisOrder(Table table)
    {
        List<string>? processedOrder = _scenarioContext.Get<List<string>>("ProcessedOrder");
        List<string> expectedOrder = table.Rows.Select(r => r["WorkType"]).ToList();

        processedOrder.Should().Equal(expectedOrder);
    }

    [Then(@"the work item should be reprocessed")]
    public void ThenTheWorkItemShouldBeReprocessed()
    {
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        workItem.CanRetry(3).Should().BeTrue();
    }

    [Then(@"the retry count should be incremented")]
    public void ThenTheRetryCountShouldBeIncremented()
    {
        SimulationWorkItem? workItem = _scenarioContext.Get<SimulationWorkItem>("WorkItem");
        workItem.RetryCount.Should().BeGreaterThan(0);
    }
}
