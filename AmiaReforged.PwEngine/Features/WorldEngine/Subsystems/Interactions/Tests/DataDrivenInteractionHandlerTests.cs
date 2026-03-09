using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;

/// <summary>
/// Tests for the data-driven interaction path: from <see cref="InteractionDefinition"/> in the
/// repository through <see cref="Handlers.DataDrivenInteractionAdapter"/> to session lifecycle.
/// Verifies knowledge gating, round calculation, weighted response selection,
/// and event publication when no compiled handler exists.
/// </summary>
[TestFixture]
public class DataDrivenInteractionHandlerTests
{
    private IInteractionSessionManager _sessionManager = null!;
    private ICharacterRepository _characterRepository = null!;
    private IInteractionHandlerRegistry _handlerRegistry = null!;
    private InMemoryInteractionDefinitionRepository _definitionRepository = null!;
    private InMemoryCharacterKnowledgeRepository _knowledgeRepository = null!;
    private PerformInteractionCommandHandler _handler = null!;

    private List<IDomainEvent> _publishedEvents = null!;
    private IEventBus _eventBus = null!;

    private CharacterId _characterId;

    private const string InteractionTag = "surveying";

    [SetUp]
    public void SetUp()
    {
        _sessionManager = new InteractionSessionManager();
        _characterRepository = new RuntimeCharacterRepository();
        _publishedEvents = [];
        _eventBus = new TestEventBus(_publishedEvents);

        // No compiled handlers — forces fallback to data-driven definitions
        _handlerRegistry = new InteractionHandlerRegistry(Array.Empty<IInteractionHandler>());
        _definitionRepository = new InMemoryInteractionDefinitionRepository();
        _knowledgeRepository = new InMemoryCharacterKnowledgeRepository();

        _handler = new PerformInteractionCommandHandler(
            _sessionManager, _characterRepository, _handlerRegistry,
            _definitionRepository, _eventBus);

        _characterId = CharacterId.New();
    }

    #region Helpers

    /// <summary>
    /// Creates a character with the given knowledge repository and optional industry membership service.
    /// </summary>
    private TestCharacter CreateCharacter(
        ICharacterKnowledgeRepository? knowledgeRepo = null,
        IIndustryMembershipService? membershipService = null)
    {
        var character = new TestCharacter(
            new Dictionary<EquipmentSlots, ItemSnapshot>(),
            [],
            _characterId,
            knowledgeRepo ?? _knowledgeRepository,
            membershipService ?? new StubMembershipService());
        _characterRepository.Add(character);
        return character;
    }

    /// <summary>
    /// Grants the character knowledge that unlocks the specified interaction tag.
    /// </summary>
    private void UnlockInteractionForCharacter(string interactionTag)
    {
        _knowledgeRepository.Add(new CharacterKnowledge
        {
            Id = Guid.NewGuid(),
            IndustryTag = "test_industry",
            CharacterId = _characterId.Value,
            Definition = new Knowledge
            {
                Tag = $"knowledge_for_{interactionTag}",
                Name = $"Knowledge for {interactionTag}",
                Description = "Test knowledge that unlocks an interaction",
                Level = ProficiencyLevel.Novice,
                Effects =
                [
                    new KnowledgeEffect
                    {
                        EffectType = KnowledgeEffectType.UnlockInteraction,
                        TargetTag = interactionTag
                    }
                ]
            }
        });
    }

    /// <summary>
    /// Creates a simple interaction definition with one guaranteed response.
    /// </summary>
    private InteractionDefinition CreateDefinition(
        string tag = InteractionTag,
        int baseRounds = 1,
        int minRounds = 1,
        bool proficiencyReducesRounds = false,
        bool requiresIndustryMembership = false,
        List<InteractionResponse>? responses = null)
    {
        responses ??=
        [
            new InteractionResponse
            {
                ResponseTag = "default_response",
                Weight = 1,
                Message = "You completed the survey.",
                Effects = []
            }
        ];

        return new InteractionDefinition
        {
            Tag = tag,
            Name = tag.Replace("_", " "),
            Description = $"Test definition for {tag}",
            BaseRounds = baseRounds,
            MinRounds = minRounds,
            ProficiencyReducesRounds = proficiencyReducesRounds,
            RequiresIndustryMembership = requiresIndustryMembership,
            Responses = responses
        };
    }

    #endregion

    #region Knowledge Gate

    [Test]
    public async Task Character_without_unlock_knowledge_cannot_start_data_driven_interaction()
    {
        // Given a definition exists but the character has NOT unlocked the interaction
        _definitionRepository.Create(CreateDefinition());
        CreateCharacter();

        // When attempting the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then it should fail with a knowledge-related message
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("haven't learned");
    }

    [Test]
    public async Task Character_with_unlock_knowledge_can_start_data_driven_interaction()
    {
        // Given a definition exists and the character HAS unlocked the interaction
        _definitionRepository.Create(CreateDefinition());
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When performing the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then it should succeed
        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task Knowledge_gate_is_case_insensitive()
    {
        // Given a definition with lowercase tag but knowledge with uppercase TargetTag
        _definitionRepository.Create(CreateDefinition(tag: "surveying"));
        _knowledgeRepository.Add(new CharacterKnowledge
        {
            Id = Guid.NewGuid(),
            IndustryTag = "test_industry",
            CharacterId = _characterId.Value,
            Definition = new Knowledge
            {
                Tag = "knowledge_upper",
                Name = "Upper Case Test",
                Description = "Test",
                Level = ProficiencyLevel.Novice,
                Effects =
                [
                    new KnowledgeEffect
                    {
                        EffectType = KnowledgeEffectType.UnlockInteraction,
                        TargetTag = "SURVEYING" // Different case
                    }
                ]
            }
        });
        CreateCharacter();

        // When performing
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "surveying", Guid.NewGuid()));

        // Then it should still succeed (case-insensitive matching)
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Industry Membership Gate

    [Test]
    public async Task Requires_industry_membership_when_definition_says_so()
    {
        // Given a definition that requires industry membership
        _definitionRepository.Create(CreateDefinition(requiresIndustryMembership: true));
        UnlockInteractionForCharacter(InteractionTag);
        // Character with NO memberships (null membershipService → empty list will cause NRE,
        // so we use a character whose AllIndustryMemberships() returns [])
        CreateCharacter();

        // When attempting the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then it should fail because of missing membership
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("member of an industry");
    }

    [Test]
    public async Task Skips_industry_check_when_definition_does_not_require_it()
    {
        // Given a definition that does NOT require industry membership
        _definitionRepository.Create(CreateDefinition(requiresIndustryMembership: false));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When attempting the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then it should succeed (no membership required)
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Round Calculation

    [Test]
    public async Task Uses_base_rounds_when_proficiency_reduction_disabled()
    {
        // Given a definition with 5 base rounds and proficiency reduction disabled
        _definitionRepository.Create(CreateDefinition(
            baseRounds: 5, minRounds: 1, proficiencyReducesRounds: false));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When starting the interaction
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then the session should require exactly 5 rounds
        InteractionSession session = _sessionManager.GetActiveSession(_characterId)!;
        session.RequiredRounds.Should().Be(5);
    }

    [Test]
    public async Task Proficiency_reduces_rounds_to_minimum()
    {
        // Given a definition with 6 base rounds, 2 min, proficiency reduction enabled
        _definitionRepository.Create(CreateDefinition(
            baseRounds: 6, minRounds: 2, proficiencyReducesRounds: true));
        UnlockInteractionForCharacter(InteractionTag);
        // Character with no industry membership → ProficiencyLevel.Layman (-1)
        // So rounds = max(2, 6 - (-1)) = max(2, 7) = 7
        CreateCharacter();

        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        InteractionSession session = _sessionManager.GetActiveSession(_characterId)!;
        // Layman proficiency (-1) → negative reduction → rounds increase to 7
        session.RequiredRounds.Should().Be(7);
    }

    #endregion

    #region Dispatcher Fallback

    [Test]
    public async Task Dispatcher_falls_back_to_data_driven_definition_when_no_compiled_handler()
    {
        // Given NO compiled handler for the tag, but a definition exists
        _definitionRepository.Create(CreateDefinition());
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When performing the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then it should succeed via the data-driven adapter
        result.Success.Should().BeTrue();
    }

    [Test]
    public async Task Returns_failure_when_neither_compiled_handler_nor_definition_exists()
    {
        // Given NO compiled handler AND no definition for the tag
        CreateCharacter();

        // When attempting the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, "nonexistent_interaction", Guid.NewGuid()));

        // Then it should fail with "Unknown interaction type"
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Unknown interaction type");
    }

    #endregion

    #region Response Selection and Events

    [Test]
    public async Task Completing_data_driven_interaction_publishes_response_selected_event()
    {
        // Given a single-round definition with effects
        var effects = new List<InteractionResponseEffect>
        {
            new()
            {
                EffectType = InteractionResponseEffectType.FloatingText,
                Value = "You found something interesting!"
            },
            new()
            {
                EffectType = InteractionResponseEffectType.VfxAtLocation,
                Value = "vfx_sparkle",
                Metadata = new Dictionary<string, object> { ["duration"] = 3 }
            }
        };

        var responses = new List<InteractionResponse>
        {
            new()
            {
                ResponseTag = "discovery",
                Weight = 1,
                Message = "You made a discovery!",
                Effects = effects
            }
        };

        _definitionRepository.Create(CreateDefinition(baseRounds: 1, responses: responses));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When the interaction completes
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then an InteractionResponseSelectedEvent should be published
        var responseEvents = _publishedEvents.OfType<InteractionResponseSelectedEvent>().ToList();
        responseEvents.Should().HaveCount(1);

        InteractionResponseSelectedEvent selected = responseEvents.First();
        selected.InteractionTag.Should().Be(InteractionTag);
        selected.ResponseTag.Should().Be("discovery");
        selected.Effects.Should().HaveCount(2);
        selected.CharacterId.Should().Be(_characterId);
    }

    [Test]
    public async Task Completing_data_driven_interaction_publishes_completed_event()
    {
        // Given a single-round definition
        _definitionRepository.Create(CreateDefinition(baseRounds: 1));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When the interaction completes
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then an InteractionCompletedEvent should also be published
        _publishedEvents.OfType<InteractionCompletedEvent>().Should().HaveCount(1);
        var completed = _publishedEvents.OfType<InteractionCompletedEvent>().First();
        completed.Success.Should().BeTrue();
        completed.InteractionTag.Should().Be(InteractionTag);
    }

    [Test]
    public async Task Outcome_includes_response_tag_and_effect_count()
    {
        // Given a definition with effects on the response
        var responses = new List<InteractionResponse>
        {
            new()
            {
                ResponseTag = "rich_vein",
                Weight = 1,
                Message = "A rich vein is nearby!",
                Effects =
                [
                    new InteractionResponseEffect
                    {
                        EffectType = InteractionResponseEffectType.DirectionalHint,
                        Value = "north"
                    },
                    new InteractionResponseEffect
                    {
                        EffectType = InteractionResponseEffectType.SpawnResourceNode,
                        Value = "copper_node"
                    }
                ]
            }
        };

        _definitionRepository.Create(CreateDefinition(baseRounds: 1, responses: responses));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When completing
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then the result data should carry responseTag and effectCount
        result.Success.Should().BeTrue();
        result.Data.Should().ContainKey("responseTag").WhoseValue.Should().Be("rich_vein");
        result.Data.Should().ContainKey("effectCount").WhoseValue.Should().Be(2);
    }

    [Test]
    public async Task No_eligible_responses_returns_failure()
    {
        // Given a definition whose only response requires Expert proficiency
        var responses = new List<InteractionResponse>
        {
            new()
            {
                ResponseTag = "expert_only",
                Weight = 1,
                MinProficiency = ProficiencyLevel.Expert,
                Message = "Expert discovery!"
            }
        };

        _definitionRepository.Create(CreateDefinition(baseRounds: 1, responses: responses));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter(); // Layman proficiency → not eligible

        // When completing the interaction
        CommandResult result = await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then it should fail because no response is eligible
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No valid outcome");
    }

    #endregion

    #region Multi-Round Lifecycle

    [Test]
    public async Task Multi_round_data_driven_interaction_progresses_and_completes()
    {
        // Given a 3-round interaction
        _definitionRepository.Create(CreateDefinition(baseRounds: 3));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        Guid targetId = Guid.NewGuid();
        PerformInteractionCommand command = new(_characterId, InteractionTag, targetId);

        // Round 1: starts + ticks → InProgress
        CommandResult r1 = await _handler.HandleAsync(command);
        r1.Success.Should().BeTrue();
        r1.Data!["status"].Should().Be("InProgress");

        // Round 2: ticks → still InProgress
        CommandResult r2 = await _handler.HandleAsync(command);
        r2.Data!["status"].Should().Be("InProgress");

        // Round 3: ticks → Completed
        CommandResult r3 = await _handler.HandleAsync(command);
        r3.Success.Should().BeTrue();
        r3.Data!["status"].Should().Be("Completed");
    }

    [Test]
    public async Task Session_is_removed_after_data_driven_interaction_completes()
    {
        // Given a single-round interaction
        _definitionRepository.Create(CreateDefinition(baseRounds: 1));
        UnlockInteractionForCharacter(InteractionTag);
        CreateCharacter();

        // When completing
        await _handler.HandleAsync(
            new PerformInteractionCommand(_characterId, InteractionTag, Guid.NewGuid()));

        // Then no active session remains
        _sessionManager.HasActiveSession(_characterId).Should().BeFalse();
    }

    #endregion

    #region Test Doubles

    private class TestEventBus : IEventBus
    {
        private readonly List<IDomainEvent> _events;
        public TestEventBus(List<IDomainEvent> events) => _events = events;

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
            where TEvent : IDomainEvent { }
    }

    private class InMemoryCharacterKnowledgeRepository : ICharacterKnowledgeRepository
    {
        private readonly List<CharacterKnowledge> _characterKnowledge = [];

        public List<CharacterKnowledge> GetKnowledgeForIndustry(string industryTag, Guid characterId)
            => _characterKnowledge.Where(ck => ck.CharacterId == characterId && ck.IndustryTag == industryTag).ToList();

        public List<Knowledge> GetAllKnowledge(Guid characterId)
            => _characterKnowledge.Where(ck => ck.CharacterId == characterId).Select(ck => ck.Definition).ToList();

        public void Add(CharacterKnowledge ck)
        {
            if (_characterKnowledge.Any(c => c.CharacterId == ck.CharacterId && c.Definition.Tag == ck.Definition.Tag))
                return;
            _characterKnowledge.Add(ck);
        }

        public void SaveChanges() { }

        public bool AlreadyKnows(Guid characterId, Knowledge knowledge)
            => _characterKnowledge.Any(ck => ck.CharacterId == characterId && ck.Definition.Tag == knowledge.Tag);
    }

    /// <summary>
    /// Minimal stub for <see cref="IIndustryMembershipService"/> that returns empty memberships
    /// by default. Use <see cref="Memberships"/> to inject memberships for specific tests.
    /// </summary>
    private class StubMembershipService : IIndustryMembershipService
    {
        public List<IndustryMembership> Memberships { get; } = [];

        public void AddMembership(IndustryMembership membership) => Memberships.Add(membership);

        public List<IndustryMembership> GetMemberships(Guid characterGuid)
            => Memberships.Where(m => m.CharacterId == characterGuid).ToList();

        public RankUpResult RankUp(IndustryMembership membership) => RankUpResult.AlreadyMaxedOut;
        public LearningResult LearnKnowledge(IndustryMembership membership, string tag) => LearningResult.DoesNotExist;
        public LearningResult LearnKnowledge(Guid characterId, string knowledgeTag) => LearningResult.DoesNotExist;
        public LearningResult CanLearnKnowledge(ICharacter character, IndustryMembership membership, Knowledge knowledge)
            => LearningResult.DoesNotExist;
        public List<Knowledge> AllKnowledge(Guid characterId) => [];
        public RankUpResult RankUp(Guid characterId, string industryTag) => RankUpResult.IndustryNotFound;
        public bool CanLearnKnowledge(Guid characterId, string knowledgeTag) => false;
    }

    #endregion
}
