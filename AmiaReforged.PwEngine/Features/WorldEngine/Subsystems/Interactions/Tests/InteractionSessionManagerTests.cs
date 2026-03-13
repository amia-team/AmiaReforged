using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;

/// <summary>
/// Unit tests for <see cref="InteractionSessionManager"/> — verifies exclusive session
/// management, session lifecycle, and metadata threading.
/// </summary>
[TestFixture]
public class InteractionSessionManagerTests
{
    private InteractionSessionManager _manager = null!;
    private CharacterId _characterId;

    [SetUp]
    public void SetUp()
    {
        _manager = new InteractionSessionManager();
        _characterId = CharacterId.New();
    }

    [Test]
    public void HasActiveSession_returns_false_when_no_session_exists()
    {
        // Given no sessions started
        // Then HasActiveSession should be false
        _manager.HasActiveSession(_characterId).Should().BeFalse();
    }

    [Test]
    public void StartSession_creates_session_with_correct_properties()
    {
        // When a session is started
        InteractionSession session = _manager.StartSession(
            _characterId, "harvesting", Guid.NewGuid(),
            InteractionTargetMode.Node, 3);

        // Then it should have the correct properties
        session.CharacterId.Should().Be(_characterId);
        session.InteractionTag.Should().Be("harvesting");
        session.TargetMode.Should().Be(InteractionTargetMode.Node);
        session.RequiredRounds.Should().Be(3);
        session.Progress.Should().Be(0);
        session.Status.Should().Be(InteractionStatus.Active);
    }

    [Test]
    public void HasActiveSession_returns_true_after_session_started()
    {
        // Given a session was started
        _manager.StartSession(_characterId, "harvesting",
            Guid.NewGuid(), InteractionTargetMode.Node, 3);

        // Then HasActiveSession should be true
        _manager.HasActiveSession(_characterId).Should().BeTrue();
    }

    [Test]
    public void GetActiveSession_returns_null_when_no_session()
    {
        // Given no sessions
        // Then GetActiveSession should return null
        _manager.GetActiveSession(_characterId).Should().BeNull();
    }

    [Test]
    public void GetActiveSession_returns_the_started_session()
    {
        // Given a session was started
        InteractionSession started = _manager.StartSession(
            _characterId, "harvesting", Guid.NewGuid(),
            InteractionTargetMode.Node, 3);

        // When getting the active session
        InteractionSession? active = _manager.GetActiveSession(_characterId);

        // Then it should be the same session
        active.Should().NotBeNull();
        active!.Id.Should().Be(started.Id);
    }

    [Test]
    public void EndSession_removes_the_active_session()
    {
        // Given an active session
        _manager.StartSession(_characterId, "harvesting",
            Guid.NewGuid(), InteractionTargetMode.Node, 3);

        // When the session is ended
        _manager.EndSession(_characterId);

        // Then there should be no active session
        _manager.HasActiveSession(_characterId).Should().BeFalse();
        _manager.GetActiveSession(_characterId).Should().BeNull();
    }

    [Test]
    public void EndSession_is_safe_when_no_session_exists()
    {
        // Given no active session
        // When EndSession is called (should not throw)
        Action act = () => _manager.EndSession(_characterId);

        // Then no exception
        act.Should().NotThrow();
    }

    [Test]
    public void StartSession_replaces_existing_session_for_same_character()
    {
        // Given an active session
        Guid target1 = Guid.NewGuid();
        _manager.StartSession(_characterId, "harvesting", target1,
            InteractionTargetMode.Node, 3);

        // When a new session is started for the same character
        Guid target2 = Guid.NewGuid();
        InteractionSession newSession = _manager.StartSession(
            _characterId, "prospecting", target2,
            InteractionTargetMode.Trigger, 5);

        // Then the active session should be the new one
        InteractionSession? active = _manager.GetActiveSession(_characterId);
        active.Should().NotBeNull();
        active!.InteractionTag.Should().Be("prospecting");
        active.TargetId.Should().Be(target2);
    }

    [Test]
    public void Sessions_are_independent_per_character()
    {
        // Given two different characters
        CharacterId char1 = CharacterId.New();
        CharacterId char2 = CharacterId.New();

        // When each starts a session
        _manager.StartSession(char1, "harvesting",
            Guid.NewGuid(), InteractionTargetMode.Node, 3);
        _manager.StartSession(char2, "prospecting",
            Guid.NewGuid(), InteractionTargetMode.Trigger, 5);

        // Then each should have their own session
        _manager.GetActiveSession(char1)!.InteractionTag.Should().Be("harvesting");
        _manager.GetActiveSession(char2)!.InteractionTag.Should().Be("prospecting");
    }

    [Test]
    public void StartSession_threads_metadata_and_area_to_session()
    {
        // Given metadata
        Dictionary<string, object> metadata = new Dictionary<string, object> { ["allowedTypes"] = "Ore" };

        // When a session is started with metadata
        InteractionSession session = _manager.StartSession(
            _characterId, "prospecting", Guid.NewGuid(),
            InteractionTargetMode.Trigger, 5, "test_area", metadata);

        // Then the session carries the metadata
        session.AreaResRef.Should().Be("test_area");
        session.Metadata.Should().ContainKey("allowedTypes");
    }

    [Test]
    public void Ending_one_characters_session_does_not_affect_others()
    {
        // Given two characters with sessions
        CharacterId char1 = CharacterId.New();
        CharacterId char2 = CharacterId.New();

        _manager.StartSession(char1, "harvesting",
            Guid.NewGuid(), InteractionTargetMode.Node, 3);
        _manager.StartSession(char2, "prospecting",
            Guid.NewGuid(), InteractionTargetMode.Trigger, 5);

        // When one session is ended
        _manager.EndSession(char1);

        // Then the other should remain
        _manager.HasActiveSession(char1).Should().BeFalse();
        _manager.HasActiveSession(char2).Should().BeTrue();
    }

    [Test]
    public void GetAllSessions_returns_empty_when_no_sessions_exist()
    {
        // Given no sessions started
        // Then GetAllSessions should return empty collection
        _manager.GetAllSessions().Should().BeEmpty();
    }

    [Test]
    public void GetAllSessions_returns_all_active_sessions()
    {
        // Given multiple characters with active sessions
        CharacterId char1 = CharacterId.New();
        CharacterId char2 = CharacterId.New();
        CharacterId char3 = CharacterId.New();

        _manager.StartSession(char1, "harvesting",
            Guid.NewGuid(), InteractionTargetMode.Node, 3);
        _manager.StartSession(char2, "prospecting",
            Guid.NewGuid(), InteractionTargetMode.Trigger, 5);
        _manager.StartSession(char3, "surveying",
            Guid.NewGuid(), InteractionTargetMode.Trigger, 2);

        // Then GetAllSessions should return all three
        IReadOnlyCollection<InteractionSession> sessions = _manager.GetAllSessions();
        sessions.Should().HaveCount(3);

        sessions.Select(s => s.InteractionTag)
            .Should().Contain(new[] { "harvesting", "prospecting", "surveying" });
    }

    [Test]
    public void GetAllSessions_reflects_ended_sessions()
    {
        // Given two sessions, one of which is ended
        CharacterId char1 = CharacterId.New();
        CharacterId char2 = CharacterId.New();

        _manager.StartSession(char1, "harvesting",
            Guid.NewGuid(), InteractionTargetMode.Node, 3);
        _manager.StartSession(char2, "prospecting",
            Guid.NewGuid(), InteractionTargetMode.Trigger, 5);

        _manager.EndSession(char1);

        // Then only the remaining session should be returned
        IReadOnlyCollection<InteractionSession> sessions = _manager.GetAllSessions();
        sessions.Should().HaveCount(1);
        sessions.First().InteractionTag.Should().Be("prospecting");
    }
}
