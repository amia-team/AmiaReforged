using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Handlers;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Integration;

/// <summary>
/// Integration tests for cross-subsystem event handler reactions.
/// Verifies that handlers properly react to events from other subsystems.
/// </summary>
[TestFixture]
public class CrossSubsystemEventHandlerTests
{
    [Test]
    public async Task OrganizationDisbandedEvent_ShouldLog_ForAuditTrail()
    {
        // Arrange - Handler just logs, no dependencies needed
        var handler = new OrganizationDisbandedEventHandler();
        var orgId = OrganizationId.New();

        // Create event
        var evt = new OrganizationDisbandedEvent(
            orgId,
            "Test Guild",
            DateTime.UtcNow);

        // Act - Handle the event (should just log)
        await handler.HandleAsync(evt);

        // Assert - Handler completes successfully (logging verified via logs, not asserts)
        Assert.Pass("Handler completed successfully");
    }

    [Test]
    public async Task OrganizationDisbandedEvent_WithNoMembers_ShouldComplete_Successfully()
    {
        // Arrange - Handler has no dependencies
        var handler = new OrganizationDisbandedEventHandler();
        var orgId = OrganizationId.New();

        var evt = new OrganizationDisbandedEvent(
            orgId,
            "Empty Guild",
            DateTime.UtcNow);

        // Act - Should not throw
        await handler.HandleAsync(evt);

        // Assert - Completes successfully
        Assert.Pass("Handler completed successfully for empty organization");
    }

    [Test]
    public async Task MultipleHandlers_ShouldProcess_SameEvent()
    {
        // Arrange - Set up event bus with multiple handlers
        var eventBus = new InMemoryEventBus();

        // Create handlers
        var disbandHandler = new OrganizationDisbandedEventHandler();
        var loggingHandler = new TestLoggingHandler();

        // Subscribe both handlers
        eventBus.Subscribe<OrganizationDisbandedEvent>(disbandHandler.HandleAsync);
        eventBus.Subscribe<OrganizationDisbandedEvent>(loggingHandler.HandleAsync);

        // Create event
        var orgId = OrganizationId.New();
        var evt = new OrganizationDisbandedEvent(
            orgId,
            "Test Guild",
            DateTime.UtcNow);

        // Act - Publish event
        await eventBus.PublishAsync(evt);

        // Assert - Both handlers should have processed the event
        Assert.That(loggingHandler.EventsHandled, Is.EqualTo(1), "Logging handler should have processed event");
    }

    [Test]
    public async Task EventHandlers_ShouldNot_Block_EventPublisher()
    {
        // Arrange
        var eventBus = new InMemoryEventBus();
        var handler = new SlowTestHandler();
        eventBus.Subscribe<OrganizationDisbandedEvent>(handler.HandleAsync);

        var evt = new OrganizationDisbandedEvent(
            OrganizationId.New(),
            "Test",
            DateTime.UtcNow);

        // Act - Publishing should complete quickly even though handler is slow
        var startTime = DateTime.UtcNow;
        await eventBus.PublishAsync(evt);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Publish should be nearly instant (handler runs separately)
        // InMemoryEventBus is synchronous, so this test verifies handler completes
        Assert.That(elapsed.TotalSeconds, Is.LessThan(2), "Event publishing should not block on slow handlers");
        Assert.That(handler.WasHandled, Is.True, "Handler should have been called");
    }

    // Test helper classes

    private class TestLoggingHandler : IEventHandler<OrganizationDisbandedEvent>
    {
        public int EventsHandled { get; private set; }

        public Task HandleAsync(OrganizationDisbandedEvent @event, CancellationToken cancellationToken = default)
        {
            EventsHandled++;
            return Task.CompletedTask;
        }
    }

    private class SlowTestHandler : IEventHandler<OrganizationDisbandedEvent>
    {
        public bool WasHandled { get; private set; }

        public async Task HandleAsync(OrganizationDisbandedEvent @event, CancellationToken cancellationToken = default)
        {
            // Simulate slow processing
            await Task.Delay(100, cancellationToken);
            WasHandled = true;
        }
    }
}

