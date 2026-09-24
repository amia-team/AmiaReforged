using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anvil.API;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations.Tests;

/// <summary>
/// Verifies that the character subsystem routes every character/context lookup
/// through <see cref="GetCharacterQuery"/> and never touches the repository
/// directly. A recording <see cref="IQueryDispatcher"/> and recording
/// <see cref="ICharacterRepository"/> make the routing and the absence of direct
/// repository access explicit. Requires no live NWN objects.
/// </summary>
[TestFixture]
public class CharacterSubsystemLookupTests
{
    private RecordingCharacterRepository _repository = null!;
    private RecordingQueryDispatcher _queries = null!;
    private ICharacterSubsystem _subsystem = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = new RecordingCharacterRepository();
        _queries = new RecordingQueryDispatcher();
        _subsystem = new CharacterSubsystem(
            _repository,
            _queries,
            new NullCommandDispatcher());
    }

    [Test]
    public async Task GetCharacterAsync_RoutesThroughGetCharacterQuery_AndReturnsKnownCharacter()
    {
        // Arrange
        CharacterId id = CharacterId.New();
        ICharacter known = new FakeCharacter(id);
        _queries.RegisterResult(new GetCharacterQuery(id), known);

        // Act
        ICharacter? result = await _subsystem.GetCharacterAsync(id);

        // Assert
        Assert.That(result, Is.SameAs(known));
        Assert.That(_queries.Dispatched, Has.Count.EqualTo(1));
        Assert.That(_queries.Dispatched[0], Is.TypeOf<GetCharacterQuery>());
        Assert.That((_queries.Dispatched[0] as GetCharacterQuery)!.CharacterId, Is.EqualTo(id));
        Assert.That(_repository.GetByIdCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task GetCharacterAsync_MissingCharacter_ReturnsNull()
    {
        // Arrange
        CharacterId id = CharacterId.New();
        _queries.RegisterResult<GetCharacterQuery, ICharacter?>(new GetCharacterQuery(id), null);

        // Act
        ICharacter? result = await _subsystem.GetCharacterAsync(id);

        // Assert
        Assert.That(result, Is.Null);
        Assert.That(_queries.Dispatched, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetKnowledgeContext_RoutesThroughGetCharacterQuery_AndReturnsContext()
    {
        // Arrange
        CharacterId id = CharacterId.New();
        ICharacter known = new FakeCharacter(id);
        _queries.RegisterResult(new GetCharacterQuery(id), known);

        // Act
        ICharacterKnowledgeContext context = _subsystem.GetKnowledgeContext(id);

        // Assert
        Assert.That(context, Is.SameAs(known));
        Assert.That(IsKnowledgeQueryDispatched());
        Assert.That(_repository.GetByIdCalls, Is.EqualTo(0));
    }

    [Test]
    public void GetIndustryContext_RoutesThroughSameGetCharacterQuery_AndReturnsContext()
    {
        // Arrange
        CharacterId id = CharacterId.New();
        ICharacter known = new FakeCharacter(id);
        _queries.RegisterResult(new GetCharacterQuery(id), known);

        // Act
        ICharacterIndustryContext context = _subsystem.GetIndustryContext(id);

        // Assert
        Assert.That(context, Is.SameAs(known));
        Assert.That(IsIndustryQueryDispatched());
        Assert.That(_repository.GetByIdCalls, Is.EqualTo(0));
    }

    [Test]
    public void GetKnowledgeContext_MissingCharacter_ThrowsInvalidOperationException()
    {
        // Arrange
        CharacterId id = CharacterId.New();
        _queries.RegisterResult<GetCharacterQuery, ICharacter?>(new GetCharacterQuery(id), null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _subsystem.GetKnowledgeContext(id));
        Assert.That(_queries.Dispatched, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetIndustryContext_MissingCharacter_ThrowsInvalidOperationException()
    {
        // Arrange
        CharacterId id = CharacterId.New();
        _queries.RegisterResult<GetCharacterQuery, ICharacter?>(new GetCharacterQuery(id), null);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _subsystem.GetIndustryContext(id));
        Assert.That(_queries.Dispatched, Has.Count.EqualTo(1));
    }

    private bool IsKnowledgeQueryDispatched() => _queries.Dispatched
        .Any(d => d is GetCharacterQuery);

    private bool IsIndustryQueryDispatched() => _queries.Dispatched
        .Any(d => d is GetCharacterQuery);

    private sealed class RecordingCharacterRepository : ICharacterRepository
    {
        private readonly Dictionary<Guid, ICharacter> _characters = new();

        public int GetByIdCalls { get; private set; }

        public void Add(ICharacter character) => _characters[character.GetId()] = character;

        public bool Exists(Guid membershipCharacterId) => _characters.ContainsKey(membershipCharacterId);

        public void Delete(ICharacter character) => _characters.Remove(character.GetId());

        public void DeleteById(Guid characterId) => _characters.Remove(characterId);

        public ICharacter? GetById(Guid characterId)
        {
            GetByIdCalls++;
            return _characters.GetValueOrDefault(characterId);
        }
    }

    private sealed class RecordingQueryDispatcher : IQueryDispatcher
    {
        public List<object> Dispatched { get; } = new();

        private readonly Dictionary<Type, object?> _results = new();

        public void RegisterResult<TQuery, TResult>(TQuery query, TResult result)
            => _results[typeof(TQuery)] = result;

        public Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
        {
            Dispatched.Add(query);
            return Task.FromResult(_results.TryGetValue(typeof(TQuery), out var r) ? (TResult)r! : default!);
        }
    }

    private sealed class NullCharacterStatRepository : ICharacterStatRepository
    {
        public CharacterStatistics? GetCharacterStatistics(Guid characterId) => null;
        public void UpdateCharacterStatistics(CharacterStatistics statistics) { }
        public void SaveChanges() { }
    }

    private sealed class NullCommandDispatcher : ICommandDispatcher
    {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand => Task.FromResult(CommandResult.Ok());

        public Task<BatchCommandResult> DispatchBatchAsync<TCommand>(
            IEnumerable<TCommand> commands,
            BatchExecutionOptions? options = null,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand => Task.FromResult(BatchCommandResult.FromResults(new List<CommandResult>()));
    }

    private sealed class FakeCharacter(CharacterId id) : ICharacter
    {
        public CharacterId GetId() => id;
        public List<SkillData> GetSkills() => new();

        public int GetKnowledgePoints() => 0;
        public void AddKnowledgePoints(int points) { }
        public void SubtractKnowledgePoints(int points) { }
        public List<Knowledge> AllKnowledge() => new();
        public LearningResult Learn(string knowledgeTag) => LearningResult.DoesNotExist;
        public bool CanLearn(string knowledgeTag) => false;
        public List<KnowledgeHarvestEffect> KnowledgeEffectsForResource(string definitionTag, ResourceType resourceType) => new();
        public void InvalidateEffectCache() { }
        public List<CraftingModifier> CraftingModifiersForRecipe(string recipeId, string industryTag) => new();
        public bool HasUnlockedInteraction(string interactionTag) => false;
        public KnowledgeProgression GetProgression() => null!;
        public void AddItem(ItemDto item) { }
        public List<ItemSnapshot> GetInventory() => [];
        public Dictionary<EquipmentSlots, ItemSnapshot?> GetEquipment() => new();
        public void JoinIndustry(string industryTag) { }
        public List<IndustryMembership> AllIndustryMemberships() => [];
        public RankUpResult RankUp(string industryTag) => default;
    }
}
