namespace WorldSimulator.Tests.Domain;

/// <summary>
/// Unit tests for the SimulationWorkItem aggregate root.
/// Following TDD/DDD principles to ensure domain invariants are enforced.
/// </summary>
[TestFixture]
public class SimulationWorkItemTests
{
    [Test]
    public void Constructor_ShouldCreateWorkItemWithPendingStatus()
    {
        // Arrange & Act
        SimulationWorkItem workItem = new SimulationWorkItem("DominionTurn", "{\"dominionId\":\"123\"}");

        // Assert
        workItem.Id.Should().NotBeEmpty();
        workItem.WorkType.Should().Be("DominionTurn");
        workItem.Payload.Should().Be("{\"dominionId\":\"123\"}");
        workItem.Status.Should().Be(WorkItemStatus.Pending);
        workItem.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        workItem.StartedAt.Should().BeNull();
        workItem.CompletedAt.Should().BeNull();
        workItem.Error.Should().BeNull();
        workItem.RetryCount.Should().Be(0);
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenWorkTypeIsNull()
    {
        // Arrange, Act & Assert
        Action act = () => new SimulationWorkItem(null!, "{}");
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Constructor_ShouldThrowArgumentNullException_WhenPayloadIsNull()
    {
        // Arrange, Act & Assert
        Action act = () => new SimulationWorkItem("Test", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Start_ShouldTransitionFromPendingToProcessing()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");

        // Act
        workItem.Start();

        // Assert
        workItem.Status.Should().Be(WorkItemStatus.Processing);
        workItem.StartedAt.Should().NotBeNull();
        workItem.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        workItem.Version.Should().Be(1);
    }

    [Test]
    public void Start_ShouldThrowInvalidOperationException_WhenAlreadyProcessing()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");
        workItem.Start();

        // Act & Assert
        Action act = () => workItem.Start();
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Complete_ShouldTransitionFromProcessingToCompleted()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");
        workItem.Start();

        // Act
        workItem.Complete();

        // Assert
        workItem.Status.Should().Be(WorkItemStatus.Completed);
        workItem.CompletedAt.Should().NotBeNull();
        workItem.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        workItem.Version.Should().Be(2);
    }

    [Test]
    public void Complete_ShouldThrowInvalidOperationException_WhenNotProcessing()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");

        // Act & Assert
        Action act = () => workItem.Complete();
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Fail_ShouldTransitionFromProcessingToFailed()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");
        workItem.Start();
        string errorMessage = "Something went wrong";

        // Act
        workItem.Fail(errorMessage);

        // Assert
        workItem.Status.Should().Be(WorkItemStatus.Failed);
        workItem.Error.Should().Be(errorMessage);
        workItem.CompletedAt.Should().NotBeNull();
        workItem.RetryCount.Should().Be(1);
        workItem.Version.Should().Be(2);
    }

    [Test]
    public void Fail_ShouldThrowInvalidOperationException_WhenNotProcessing()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");

        // Act & Assert
        Action act = () => workItem.Fail("Error");
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Cancel_ShouldTransitionToCancelled()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");

        // Act
        workItem.Cancel();

        // Assert
        workItem.Status.Should().Be(WorkItemStatus.Cancelled);
        workItem.CompletedAt.Should().NotBeNull();
        workItem.Version.Should().Be(1);
    }

    [Test]
    public void Cancel_ShouldThrowInvalidOperationException_WhenAlreadyCompleted()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");
        workItem.Start();
        workItem.Complete();

        // Act & Assert
        Action act = () => workItem.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void CanRetry_ShouldReturnTrue_WhenFailedAndBelowMaxRetries()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");
        workItem.Start();
        workItem.Fail("Error");

        // Act & Assert
        workItem.CanRetry(3).Should().BeTrue();
    }

    [Test]
    public void CanRetry_ShouldReturnFalse_WhenFailedAndAtMaxRetries()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");
        workItem.Start();
        workItem.Fail("Error 1");
        workItem.Start();
        workItem.Fail("Error 2");
        workItem.Start();
        workItem.Fail("Error 3");

        // Act & Assert
        workItem.CanRetry(3).Should().BeFalse();
    }

    [Test]
    public void CanRetry_ShouldReturnFalse_WhenNotFailed()
    {
        // Arrange
        SimulationWorkItem workItem = new SimulationWorkItem("Test", "{}");

        // Act & Assert
        workItem.CanRetry(3).Should().BeFalse();
    }
}

