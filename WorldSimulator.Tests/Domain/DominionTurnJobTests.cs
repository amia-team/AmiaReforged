namespace WorldSimulator.Tests.Domain;

/// <summary>
/// Unit tests for DominionTurnJob aggregate following BDD principles.
/// Tests capture BEHAVIOR, not implementation details.
/// </summary>
[TestFixture]
public class DominionTurnJobTests
{
    [TestFixture]
    public class WhenCreatingDominionTurnJob
    {
        [Test]
        public void ShouldCreateJobInQueuedStatus()
        {
            // Arrange
            Guid governmentId = Guid.NewGuid();
            string governmentName = "Kingdom of Amia";
            DateTime turnDate = new DateTime(2025, 10, 29);
            int totalScenarios = 5;

            // Act
            DominionTurnJob job = new DominionTurnJob(governmentId, governmentName, turnDate, totalScenarios);

            // Assert
            job.Should().NotBeNull();
            job.Id.Should().NotBe(Guid.Empty);
            job.GovernmentId.Should().Be(governmentId);
            job.GovernmentName.Should().Be(governmentName);
            job.TurnDate.Should().Be(turnDate);
            job.TotalScenarios.Should().Be(totalScenarios);
            job.Status.Should().Be(DominionTurnStatus.Queued);
            job.ScenariosProcessed.Should().Be(0);
            job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void ShouldThrowWhenGovernmentIdIsEmpty()
        {
            // Act
            Action act = () => new DominionTurnJob(Guid.Empty, "Test", DateTime.UtcNow, 5);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Government ID cannot be empty*");
        }

        [Test]
        public void ShouldThrowWhenGovernmentNameIsEmpty()
        {
            // Act
            Action act = () => new DominionTurnJob(Guid.NewGuid(), "", DateTime.UtcNow, 5);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Government name is required*");
        }

        [Test]
        public void ShouldThrowWhenTotalScenariosIsZero()
        {
            // Act
            Action act = () => new DominionTurnJob(Guid.NewGuid(), "Test", DateTime.UtcNow, 0);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Total scenarios must be greater than zero*");
        }
    }

    [TestFixture]
    public class WhenStartingDominionTurnJob
    {
        [Test]
        public void ShouldTransitionFromQueuedToRunning()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            uint initialVersion = job.Version;

            // Act
            job.Start();

            // Assert
            job.Status.Should().Be(DominionTurnStatus.Running);
            job.StartedAt.Should().NotBeNull();
            job.StartedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            job.Version.Should().Be(initialVersion + 1);
        }

        [Test]
        public void ShouldThrowWhenAlreadyRunning()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            job.Start();

            // Act
            Action act = () => job.Start();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot start job in status Running*");
        }

        [Test]
        public void ShouldThrowWhenAlreadyCompleted()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 1);
            job.Start();
            job.RecordScenarioCompleted();
            job.Complete();

            // Act
            Action act = () => job.Start();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot start job in status Completed*");
        }
    }

    [TestFixture]
    public class WhenRecordingScenarioProgress
    {
        [Test]
        public void ShouldIncrementScenariosProcessed()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            job.Start();
            uint initialVersion = job.Version;

            // Act
            job.RecordScenarioCompleted();

            // Assert
            job.ScenariosProcessed.Should().Be(1);
            job.Version.Should().Be(initialVersion + 1);
        }

        [Test]
        public void ShouldAllowRecordingMultipleScenarios()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            job.Start();

            // Act
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();

            // Assert
            job.ScenariosProcessed.Should().Be(3);
            job.GetProgressPercentage().Should().Be(60);
        }

        [Test]
        public void ShouldThrowWhenNotRunning()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);

            // Act
            Action act = () => job.RecordScenarioCompleted();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot record progress when job is Queued*");
        }
    }

    [TestFixture]
    public class WhenCompletingDominionTurnJob
    {
        [Test]
        public void ShouldTransitionToCompletedWhenAllScenariosProcessed()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 3);
            job.Start();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();
            uint initialVersion = job.Version;

            // Act
            job.Complete();

            // Assert
            job.Status.Should().Be(DominionTurnStatus.Completed);
            job.CompletedAt.Should().NotBeNull();
            job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            job.Version.Should().Be(initialVersion + 1);
            job.IsComplete().Should().BeTrue();
        }

        [Test]
        public void ShouldThrowWhenNotAllScenariosProcessed()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            job.Start();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();

            // Act
            Action act = () => job.Complete();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*only 2/5 scenarios processed*");
        }

        [Test]
        public void ShouldThrowWhenNotRunning()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);

            // Act
            Action act = () => job.Complete();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot complete job in status Queued*");
        }
    }

    [TestFixture]
    public class WhenFailingDominionTurnJob
    {
        [Test]
        public void ShouldTransitionToFailedWithErrorMessage()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            job.Start();
            string errorMessage = "Database connection failed";
            uint initialVersion = job.Version;

            // Act
            job.Fail(errorMessage);

            // Assert
            job.Status.Should().Be(DominionTurnStatus.Failed);
            job.ErrorMessage.Should().Be(errorMessage);
            job.CompletedAt.Should().NotBeNull();
            job.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            job.Version.Should().Be(initialVersion + 1);
        }

        [Test]
        public void ShouldThrowWhenErrorMessageIsEmpty()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            job.Start();

            // Act
            Action act = () => job.Fail("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Error message is required*");
        }

        [Test]
        public void ShouldThrowWhenNotRunning()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);

            // Act
            Action act = () => job.Fail("Some error");

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Cannot fail job in status Queued*");
        }
    }

    [TestFixture]
    public class WhenCheckingJobProgress
    {
        [Test]
        public void IsCompleteShouldReturnTrueWhenAllScenariosProcessed()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 3);
            job.Start();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();

            // Act & Assert
            job.IsComplete().Should().BeTrue();
        }

        [Test]
        public void IsCompleteShouldReturnFalseWhenScenariosRemaining()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);
            job.Start();
            job.RecordScenarioCompleted();

            // Act & Assert
            job.IsComplete().Should().BeFalse();
        }

        [Test]
        public void GetProgressPercentageShouldCalculateCorrectly()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 10);
            job.Start();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();
            job.RecordScenarioCompleted();

            // Act
            decimal percentage = job.GetProgressPercentage();

            // Assert
            percentage.Should().Be(30);
        }

        [Test]
        public void GetProgressPercentageShouldReturnZeroWhenNoScenariosProcessed()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(Guid.NewGuid(), "Test Kingdom", DateTime.UtcNow, 5);

            // Act
            decimal percentage = job.GetProgressPercentage();

            // Assert
            percentage.Should().Be(0);
        }
    }
}

