using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Commands;

/// <summary>
/// Behavioral specifications for BatchExecutionOptions value object.
/// Tests follow BDD-style naming and focus on declarative behavior.
/// </summary>
[TestFixture]
public class BatchExecutionOptionsBehavior
{
    // === Default Behavior ===

    [Test]
    public void GivenDefaultOptions_ThenStopsOnFirstFailure()
    {
        // Given/When: Default options are created
        BatchExecutionOptions options = BatchExecutionOptions.Default;

        // Then: Stop on first failure is enabled
        Assert.That(options.StopOnFirstFailure, Is.True);
        Assert.That(options.UseTransaction, Is.False);
        Assert.That(options.MaxDegreeOfParallelism, Is.EqualTo(1));
    }

    // === Factory Methods ===

    [Test]
    public void GivenContinueOnFailureOption_ThenDoesNotStopOnFailure()
    {
        // Given/When: ContinueOnFailure options are created
        BatchExecutionOptions options = BatchExecutionOptions.ContinueOnFailure();

        // Then: Stop on first failure is disabled
        Assert.That(options.StopOnFirstFailure, Is.False);
    }

    [Test]
    public void GivenTransactionalOption_ThenEnablesTransactionAndStopsOnFailure()
    {
        // Given/When: Transactional options are created
        BatchExecutionOptions options = BatchExecutionOptions.Transactional();

        // Then: Transaction is enabled and stops on failure
        Assert.That(options.UseTransaction, Is.True);
        Assert.That(options.StopOnFirstFailure, Is.True);
    }

    [Test]
    public void GivenParallelOption_ThenSetsMaxDegreeOfParallelism()
    {
        // Given: A desired parallelism level
        const int parallelism = 4;

        // When: Parallel options are created
        BatchExecutionOptions options = BatchExecutionOptions.Parallel(parallelism);

        // Then: Max degree of parallelism is set correctly
        Assert.That(options.MaxDegreeOfParallelism, Is.EqualTo(parallelism));
    }

    // === Value Object Equality ===

    [Test]
    public void GivenTwoOptionsWithSameValues_ThenTheyAreEqual()
    {
        // Given: Two options with identical values
        BatchExecutionOptions options1 = new BatchExecutionOptions
        {
            StopOnFirstFailure = true,
            UseTransaction = false,
            MaxDegreeOfParallelism = 1
        };
        BatchExecutionOptions options2 = new BatchExecutionOptions
        {
            StopOnFirstFailure = true,
            UseTransaction = false,
            MaxDegreeOfParallelism = 1
        };

        // When/Then: They are considered equal
        Assert.That(options1, Is.EqualTo(options2));
        Assert.That(options1.GetHashCode(), Is.EqualTo(options2.GetHashCode()));
    }

    [Test]
    public void GivenTwoOptionsWithDifferentValues_ThenTheyAreNotEqual()
    {
        // Given: Two options with different values
        BatchExecutionOptions options1 = BatchExecutionOptions.Default;
        BatchExecutionOptions options2 = BatchExecutionOptions.ContinueOnFailure();

        // When/Then: They are not equal
        Assert.That(options1, Is.Not.EqualTo(options2));
    }

    // === Immutability ===

    [Test]
    public void GivenOptions_WhenModifiedViaWith_ThenOriginalRemainsUnchanged()
    {
        // Given: Original options
        BatchExecutionOptions original = BatchExecutionOptions.Default;

        // When: Modified via with expression
        BatchExecutionOptions modified = original with { StopOnFirstFailure = false };

        // Then: Original is unchanged, modified is different
        Assert.That(original.StopOnFirstFailure, Is.True);
        Assert.That(modified.StopOnFirstFailure, Is.False);
        Assert.That(original, Is.Not.EqualTo(modified));
    }
}

/// <summary>
/// Behavioral specifications for BatchCommandResult value object.
/// </summary>
[TestFixture]
public class BatchCommandResultBehavior
{
    // === Result Creation ===

    [Test]
    public void GivenSuccessfulResults_WhenCreated_ThenCountsAreCorrect()
    {
        // Given: A list of successful command results
        List<CommandResult> results = new List<CommandResult>
        {
            CommandResult.Ok(),
            CommandResult.Ok(),
            CommandResult.Ok()
        };

        // When: BatchCommandResult is created
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        // Then: Counts are correct
        Assert.That(batchResult.TotalCount, Is.EqualTo(3));
        Assert.That(batchResult.SuccessCount, Is.EqualTo(3));
        Assert.That(batchResult.FailedCount, Is.EqualTo(0));
        Assert.That(batchResult.AllSucceeded, Is.True);
        Assert.That(batchResult.AnyFailed, Is.False);
    }

    [Test]
    public void GivenMixedResults_WhenCreated_ThenCountsReflectActualOutcomes()
    {
        // Given: A mix of successful and failed results
        List<CommandResult> results = new List<CommandResult>
        {
            CommandResult.Ok(),
            CommandResult.Fail("error 1"),
            CommandResult.Ok(),
            CommandResult.Fail("error 2")
        };

        // When: BatchCommandResult is created
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        // Then: Counts reflect the mix
        Assert.That(batchResult.TotalCount, Is.EqualTo(4));
        Assert.That(batchResult.SuccessCount, Is.EqualTo(2));
        Assert.That(batchResult.FailedCount, Is.EqualTo(2));
        Assert.That(batchResult.AllSucceeded, Is.False);
        Assert.That(batchResult.AnyFailed, Is.True);
    }

    [Test]
    public void GivenCancelledBatch_WhenCreated_ThenCancelledFlagIsSet()
    {
        // Given: A partial result list with cancelled flag
        List<CommandResult> results = new List<CommandResult>
        {
            CommandResult.Ok(),
            CommandResult.Ok()
        };

        // When: BatchCommandResult is created with cancelled flag
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results, cancelled: true);

        // Then: Cancelled flag is set and AllSucceeded is false
        Assert.That(batchResult.Cancelled, Is.True);
        Assert.That(batchResult.AllSucceeded, Is.False);
    }

    [Test]
    public void GivenEmptyBatch_WhenCreated_ThenCountsAreZero()
    {
        // Given/When: Empty batch result
        BatchCommandResult batchResult = BatchCommandResult.Empty;

        // Then: All counts are zero
        Assert.That(batchResult.TotalCount, Is.EqualTo(0));
        Assert.That(batchResult.SuccessCount, Is.EqualTo(0));
        Assert.That(batchResult.FailedCount, Is.EqualTo(0));
        Assert.That(batchResult.AllSucceeded, Is.True); // Vacuously true
        Assert.That(batchResult.AnyFailed, Is.False);
    }

    // === Success Rate Calculations ===

    [Test]
    public void GivenAllSuccessful_ThenSuccessRateIs100Percent()
    {
        // Given: All successful results
        List<CommandResult> results = Enumerable.Repeat(CommandResult.Ok(), 5).ToList();

        // When: BatchCommandResult is created
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        // Then: Success rate is 100%
        Assert.That(batchResult.SuccessRate, Is.EqualTo(100.0));
    }

    [Test]
    public void GivenAllFailed_ThenSuccessRateIsZeroPercent()
    {
        // Given: All failed results
        List<CommandResult> results = Enumerable.Repeat(CommandResult.Fail("error"), 5).ToList();

        // When: BatchCommandResult is created
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        // Then: Success rate is 0%
        Assert.That(batchResult.SuccessRate, Is.EqualTo(0.0));
    }

    [Test]
    public void GivenHalfSuccessful_ThenSuccessRateIs50Percent()
    {
        // Given: Half successful, half failed
        List<CommandResult> results = new List<CommandResult>
        {
            CommandResult.Ok(),
            CommandResult.Fail("error"),
            CommandResult.Ok(),
            CommandResult.Fail("error")
        };

        // When: BatchCommandResult is created
        BatchCommandResult batchResult = BatchCommandResult.FromResults(results);

        // Then: Success rate is 50%
        Assert.That(batchResult.SuccessRate, Is.EqualTo(50.0));
    }

    [Test]
    public void GivenEmptyResults_ThenSuccessRateIsZero()
    {
        // Given/When: Empty results
        BatchCommandResult batchResult = BatchCommandResult.Empty;

        // Then: Success rate is 0 (not undefined)
        Assert.That(batchResult.SuccessRate, Is.EqualTo(0.0));
    }

    // === Value Object Equality ===

    [Test]
    public void GivenTwoResultsWithSameValues_ThenTheyAreEqual()
    {
        // Given: Two batch results with identical properties
        List<CommandResult> results = new List<CommandResult> { CommandResult.Ok() };
        BatchCommandResult batchResult1 = BatchCommandResult.FromResults(results);
        BatchCommandResult batchResult2 = BatchCommandResult.FromResults(results);

        // When/Then: They are considered equal
        Assert.That(batchResult1, Is.EqualTo(batchResult2));
    }

    [Test]
    public void GivenTwoResultsWithDifferentValues_ThenTheyAreNotEqual()
    {
        // Given: Two batch results with different properties
        List<CommandResult> results1 = new List<CommandResult> { CommandResult.Ok() };
        List<CommandResult> results2 = new List<CommandResult> { CommandResult.Fail("error") };
        BatchCommandResult batchResult1 = BatchCommandResult.FromResults(results1);
        BatchCommandResult batchResult2 = BatchCommandResult.FromResults(results2);

        // When/Then: They are not equal
        Assert.That(batchResult1, Is.Not.EqualTo(batchResult2));
    }
}

