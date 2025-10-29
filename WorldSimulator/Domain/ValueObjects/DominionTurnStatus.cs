namespace WorldSimulator.Domain.ValueObjects
{
    /// <summary>
    /// Represents the lifecycle state of a dominion turn job
    /// </summary>
    public enum DominionTurnStatus
    {
        /// <summary>Job has been created and is waiting to be processed</summary>
        Queued = 0,

        /// <summary>Job is currently being processed</summary>
        Running = 1,

        /// <summary>Job completed successfully</summary>
        Completed = 2,

        /// <summary>Job failed during processing</summary>
        Failed = 3
    }
}

namespace WorldSimulator.Domain.Aggregates
{
    /// <summary>
    /// Aggregate root representing a dominion turn execution job.
    /// Orchestrates the processing of government hierarchy: Territory → Region → Settlement
    /// </summary>
    public class DominionTurnJob
    {
        /// <summary>Unique identifier for this job</summary>
        public Guid Id { get; private set; }

        /// <summary>Government this turn belongs to</summary>
        public Guid GovernmentId { get; private set; }

        /// <summary>Human-readable government name for logging/display</summary>
        public string GovernmentName { get; private set; }

        /// <summary>Turn date being processed</summary>
        public DateTime TurnDate { get; private set; }

        /// <summary>Current status of the job</summary>
        public DominionTurnStatus Status { get; private set; }

        /// <summary>When the job was created</summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>When processing started</summary>
        public DateTime? StartedAt { get; private set; }

        /// <summary>When processing completed</summary>
        public DateTime? CompletedAt { get; private set; }

        /// <summary>Error details if failed</summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>Number of scenarios processed</summary>
        public int ScenariosProcessed { get; private set; }

        /// <summary>Total scenarios to process</summary>
        public int TotalScenarios { get; private set; }

        /// <summary>Concurrency token</summary>
        public uint Version { get; private set; }

        private DominionTurnJob() { } // EF Core

        public DominionTurnJob(Guid governmentId, string governmentName, DateTime turnDate, int totalScenarios)
        {
            if (governmentId == Guid.Empty)
                throw new ArgumentException("Government ID cannot be empty", nameof(governmentId));

            if (string.IsNullOrWhiteSpace(governmentName))
                throw new ArgumentException("Government name is required", nameof(governmentName));

            if (totalScenarios <= 0)
                throw new ArgumentException("Total scenarios must be greater than zero", nameof(totalScenarios));

            Id = Guid.NewGuid();
            GovernmentId = governmentId;
            GovernmentName = governmentName;
            TurnDate = turnDate;
            TotalScenarios = totalScenarios;
            Status = DominionTurnStatus.Queued;
            CreatedAt = DateTime.UtcNow;
            ScenariosProcessed = 0;
            Version = 0;
        }

        /// <summary>
        /// Starts the dominion turn job
        /// </summary>
        public void Start()
        {
            if (Status != DominionTurnStatus.Queued)
                throw new InvalidOperationException($"Cannot start job in status {Status}");

            Status = DominionTurnStatus.Running;
            StartedAt = DateTime.UtcNow;
            Version++;
        }

        /// <summary>
        /// Records progress of a completed scenario
        /// </summary>
        public void RecordScenarioCompleted()
        {
            if (Status != DominionTurnStatus.Running)
                throw new InvalidOperationException($"Cannot record progress when job is {Status}");

            ScenariosProcessed++;
            Version++;
        }

        /// <summary>
        /// Completes the dominion turn job successfully
        /// </summary>
        public void Complete()
        {
            if (Status != DominionTurnStatus.Running)
                throw new InvalidOperationException($"Cannot complete job in status {Status}");

            if (ScenariosProcessed < TotalScenarios)
                throw new InvalidOperationException($"Cannot complete job - only {ScenariosProcessed}/{TotalScenarios} scenarios processed");

            Status = DominionTurnStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            Version++;
        }

        /// <summary>
        /// Marks the job as failed with error details
        /// </summary>
        public void Fail(string errorMessage)
        {
            if (Status != DominionTurnStatus.Running)
                throw new InvalidOperationException($"Cannot fail job in status {Status}");

            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required", nameof(errorMessage));

            Status = DominionTurnStatus.Failed;
            ErrorMessage = errorMessage;
            CompletedAt = DateTime.UtcNow;
            Version++;
        }

        /// <summary>
        /// Checks if all scenarios have been processed
        /// </summary>
        public bool IsComplete() => ScenariosProcessed >= TotalScenarios;

        /// <summary>
        /// Gets progress percentage
        /// </summary>
        public decimal GetProgressPercentage() =>
            TotalScenarios > 0 ? (decimal)ScenariosProcessed / TotalScenarios * 100 : 0;
    }
}

