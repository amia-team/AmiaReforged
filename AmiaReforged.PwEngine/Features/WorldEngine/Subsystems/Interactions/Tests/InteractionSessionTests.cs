using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;

/// <summary>
/// Unit tests for <see cref="InteractionSession"/> — verifies progress tracking,
/// completion detection, and session metadata.
/// </summary>
[TestFixture]
public class InteractionSessionTests
{
    private InteractionSession CreateSession(int requiredRounds = 3)
    {
        return new InteractionSession
        {
            CharacterId = CharacterId.New(),
            InteractionTag = "test_interaction",
            TargetId = Guid.NewGuid(),
            TargetMode = InteractionTargetMode.Node,
            RequiredRounds = requiredRounds
        };
    }

    [Test]
    public void New_session_starts_with_zero_progress()
    {
        // Given a newly created session
        InteractionSession session = CreateSession();

        // Then progress should be zero
        session.Progress.Should().Be(0);
    }

    [Test]
    public void New_session_has_active_status()
    {
        // Given a newly created session
        InteractionSession session = CreateSession();

        // Then status should be active
        session.Status.Should().Be(InteractionStatus.Active);
    }

    [Test]
    public void IncrementProgress_advances_by_specified_amount()
    {
        // Given a session with 3 required rounds
        InteractionSession session = CreateSession(requiredRounds: 3);

        // When progress is incremented by 1
        int newProgress = session.IncrementProgress(1);

        // Then progress should be 1
        newProgress.Should().Be(1);
        session.Progress.Should().Be(1);
    }

    [Test]
    public void IncrementProgress_accumulates_across_calls()
    {
        // Given a session with 5 required rounds
        InteractionSession session = CreateSession(requiredRounds: 5);

        // When progress is incremented multiple times
        session.IncrementProgress(1);
        session.IncrementProgress(2);
        int finalProgress = session.IncrementProgress(1);

        // Then progress should reflect the total
        finalProgress.Should().Be(4);
    }

    [Test]
    public void IsComplete_returns_false_when_below_required_rounds()
    {
        // Given a session with 3 required rounds
        InteractionSession session = CreateSession(requiredRounds: 3);

        // When progress is partially done
        session.IncrementProgress(2);

        // Then it should not be complete
        session.IsComplete.Should().BeFalse();
    }

    [Test]
    public void IsComplete_returns_true_when_progress_equals_required_rounds()
    {
        // Given a session with 3 required rounds
        InteractionSession session = CreateSession(requiredRounds: 3);

        // When progress reaches required rounds
        session.IncrementProgress(3);

        // Then it should be complete
        session.IsComplete.Should().BeTrue();
    }

    [Test]
    public void IsComplete_returns_true_when_progress_exceeds_required_rounds()
    {
        // Given a session with 2 required rounds
        InteractionSession session = CreateSession(requiredRounds: 2);

        // When progress overshoots due to bonus
        session.IncrementProgress(5);

        // Then it should be complete
        session.IsComplete.Should().BeTrue();
    }

    [Test]
    public void Session_carries_metadata()
    {
        // Given a session with metadata
        InteractionSession session = new()
        {
            CharacterId = CharacterId.New(),
            InteractionTag = "prospecting",
            TargetId = Guid.NewGuid(),
            TargetMode = InteractionTargetMode.Trigger,
            RequiredRounds = 5,
            AreaResRef = "test_area",
            Metadata = new Dictionary<string, object> { ["allowedTypes"] = "Ore,Geode" }
        };

        // Then metadata should be accessible
        session.AreaResRef.Should().Be("test_area");
        session.Metadata.Should().ContainKey("allowedTypes");
        session.Metadata!["allowedTypes"].Should().Be("Ore,Geode");
    }

    [Test]
    public void Session_has_unique_id()
    {
        // Given two sessions
        InteractionSession session1 = CreateSession();
        InteractionSession session2 = CreateSession();

        // Then they should have different IDs
        session1.Id.Should().NotBe(session2.Id);
    }
}
