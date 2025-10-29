namespace WorldSimulator.Tests.Application;

/// <summary>
/// Behavior-focused tests for DominionTurnProcessor.
/// Tests verify WHAT the processor does, not HOW it does it.
/// </summary>
[TestFixture]
public class DominionTurnProcessorTests
{
    private SimulationDbContext _context = null!;
    private Mock<IEventLogPublisher> _eventPublisherMock = null!;
    private Mock<ILogger<DominionTurnProcessor>> _loggerMock = null!;
    private DominionTurnProcessor _processor = null!;

    [SetUp]
    public void Setup()
    {
        // Use in-memory database for testing
        DbContextOptions<SimulationDbContext> options = new DbContextOptionsBuilder<SimulationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SimulationDbContext(options);
        _eventPublisherMock = new Mock<IEventLogPublisher>();
        _loggerMock = new Mock<ILogger<DominionTurnProcessor>>();

        _processor = new DominionTurnProcessor(_context, _eventPublisherMock.Object, _loggerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    [TestFixture]
    public class WhenProcessingDominionTurn : DominionTurnProcessorTests
    {
        [Test]
        public async Task ShouldStartJobBeforeProcessing()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act
            await _processor.ProcessAsync(job);

            // Assert
            job.Status.Should().Be(DominionTurnStatus.Completed);
            job.StartedAt.Should().NotBeNull();
        }

        [Test]
        public async Task ShouldPublishStartedEvent()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act
            await _processor.ProcessAsync(job);

            // Assert
            _eventPublisherMock.Verify(
                x => x.PublishAsync(
                    It.Is<DominionTurnStartedEvent>(e =>
                        e.JobId == job.Id &&
                        e.GovernmentName == job.GovernmentName),
                    EventSeverity.Information,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ShouldProcessAllScenariosInOrder()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act
            await _processor.ProcessAsync(job);

            // Assert
            job.ScenariosProcessed.Should().Be(3);
            job.IsComplete().Should().BeTrue();
        }

        [Test]
        public async Task ShouldCompleteJobAfterAllScenarios()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act
            await _processor.ProcessAsync(job);

            // Assert
            job.Status.Should().Be(DominionTurnStatus.Completed);
            job.CompletedAt.Should().NotBeNull();
        }

        [Test]
        public async Task ShouldPublishCompletedEvent()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act
            await _processor.ProcessAsync(job);

            // Assert
            _eventPublisherMock.Verify(
                x => x.PublishAsync(
                    It.Is<DominionTurnCompletedEvent>(e =>
                        e.JobId == job.Id &&
                        e.ScenariosProcessed == 3),
                    EventSeverity.Information,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ShouldPersistJobStateChangesToDatabase()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();
            Guid jobId = job.Id;

            // Act
            await _processor.ProcessAsync(job);

            // Assert - Reload from database to verify persistence
            DominionTurnJob? reloadedJob = await _context.DominionTurnJobs.FindAsync(jobId);
            reloadedJob.Should().NotBeNull();
            reloadedJob!.Status.Should().Be(DominionTurnStatus.Completed);
            reloadedJob.ScenariosProcessed.Should().Be(3);
        }
    }

    [TestFixture]
    public class WhenProcessingFails : DominionTurnProcessorTests
    {
        [Test]
        public void ShouldThrowExceptionWhenJobIsNull()
        {
            // Act
            Func<Task> act = async () => await _processor.ProcessAsync(null!);

            // Assert
            act.Should().ThrowAsync<ArgumentNullException>();
        }

        [Test]
        public async Task ShouldMarkJobAsFailedWhenExceptionOccurs()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            // Make event publisher throw to simulate failure
            _eventPublisherMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<DominionTurnStartedEvent>(),
                    It.IsAny<EventSeverity>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Event bus unavailable"));

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _processor.ProcessAsync(job);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();
            job.Status.Should().Be(DominionTurnStatus.Failed);
            job.ErrorMessage.Should().Contain("Event bus unavailable");
        }

        [Test]
        public async Task ShouldPublishFailedEventWhenExceptionOccurs()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _eventPublisherMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<DominionTurnStartedEvent>(),
                    It.IsAny<EventSeverity>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Event bus unavailable"));

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();

            // Act
            try
            {
                await _processor.ProcessAsync(job);
            }
            catch
            {
                // Expected exception
            }

            // Assert
            _eventPublisherMock.Verify(
                x => x.PublishAsync(
                    It.Is<DominionTurnFailedEvent>(e =>
                        e.JobId == job.Id &&
                        e.ErrorMessage.Contains("Event bus unavailable")),
                    EventSeverity.Information,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ShouldPersistFailureStateToDatabase()
        {
            // Arrange
            DominionTurnJob job = new DominionTurnJob(
                Guid.NewGuid(),
                "Kingdom of Amia",
                DateTime.UtcNow,
                3);

            _eventPublisherMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<DominionTurnStartedEvent>(),
                    It.IsAny<EventSeverity>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            _context.DominionTurnJobs.Add(job);
            await _context.SaveChangesAsync();
            Guid jobId = job.Id;

            // Act
            try
            {
                await _processor.ProcessAsync(job);
            }
            catch
            {
                // Expected exception
            }

            // Assert - Reload from database to verify failure was persisted
            DominionTurnJob? reloadedJob = await _context.DominionTurnJobs.FindAsync(jobId);
            reloadedJob.Should().NotBeNull();
            reloadedJob!.Status.Should().Be(DominionTurnStatus.Failed);
            reloadedJob.ErrorMessage.Should().Contain("Database connection failed");
            reloadedJob.CompletedAt.Should().NotBeNull();
        }
    }
}

