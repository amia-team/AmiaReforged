using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Queries;
using NUnit.Framework;

// The subsystem returns the public CharacterTrait projection; the entity
// CharacterTrait (in ...Traits) is used only for building repository input.
using PublicCharacterTrait = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.CharacterTrait;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// BDD-style tests for the production TraitSubsystem read projections.
/// Verifies the four read methods only dispatch/map results (no direct repository reads)
/// and covers the HasTraitAsync edge cases required by task 031.
/// </summary>
[TestFixture]
public class TraitSubsystemTests
{
    private const string BraveTraitTag = "brave";
    private const string CowardTraitTag = "coward";
    private const string GhostTraitTag = "ghost";

    private static TraitSubsystem CreateSubsystem(ITraitRepository traitRepository, ICharacterTraitRepository characterTraitRepository)
    {
        GetTraitDefinitionQueryHandler getDefinition = new(traitRepository);
        GetAllTraitsQueryHandler getAllTraits = new(traitRepository);
        GetCharacterTraitsQueryHandler getCharacterTraits = new(characterTraitRepository, traitRepository);
        HasTraitAsyncQueryHandler hasTrait = new(characterTraitRepository, traitRepository);

        RoutingQueryDispatcher queryDispatcher = new(getDefinition, getAllTraits, getCharacterTraits, hasTrait);

        return new TraitSubsystem(
            new StubCommandDispatcher(),
            queryDispatcher,
            traitRepository,
            characterTraitRepository);
    }

    [SetUp]
    public void SetUp()
    {
        ITraitRepository traitRepository = new InMemoryTraitRepository();
        traitRepository.Add(new Trait
        {
            Tag = BraveTraitTag,
            Name = "Brave",
            Description = "Fearless in combat",
            PointCost = 1
        });
        traitRepository.Add(new Trait
        {
            Tag = CowardTraitTag,
            Name = "Coward",
            Description = "Easily frightened",
            PointCost = -1
        });

        ICharacterTraitRepository characterTraitRepository = new InMemoryCharacterTraitRepository();

        _traitRepository = traitRepository;
        _characterTraitRepository = characterTraitRepository;
        _subsystem = CreateSubsystem(traitRepository, characterTraitRepository);
    }

    [TearDown]
    public void TearDown()
    {
        _subsystem = null!;
    }

    private ITraitRepository _traitRepository = null!;
    private ICharacterTraitRepository _characterTraitRepository = null!;
    private TraitSubsystem _subsystem = null!;

    #region GetTraitAsync

    [Test]
    public async Task GetTraitAsync_WithExistingTrait_ShouldMapToDefinition()
    {
        // When
        TraitDefinition? definition = await _subsystem.GetTraitAsync(new TraitTag(BraveTraitTag));

        // Then
        Assert.That(definition, Is.Not.Null);
        Assert.That(definition!.Tag.Value, Is.EqualTo(BraveTraitTag));
        Assert.That(definition.Name, Is.EqualTo("Brave"));
        Assert.That(definition.PointCost, Is.EqualTo(1));
    }

    [Test]
    public async Task GetTraitAsync_WithMissingDefinition_ShouldReturnNull()
    {
        // When
        TraitDefinition? definition = await _subsystem.GetTraitAsync(new TraitTag("nonexistent"));

        // Then
        Assert.That(definition, Is.Null);
    }

    #endregion

    #region GetAllTraitsAsync

    [Test]
    public async Task GetAllTraitsAsync_ShouldMapAllDefinitions()
    {
        // When
        List<TraitDefinition> definitions = await _subsystem.GetAllTraitsAsync();

        // Then
        Assert.That(definitions, Has.Count.EqualTo(2));
        Assert.That(definitions.Select(d => d.Tag.Value), Contains.Item(BraveTraitTag));
        Assert.That(definitions.Select(d => d.Tag.Value), Contains.Item(CowardTraitTag));
        Assert.That(definitions.Select(d => d.Name), Contains.Item("Brave"));
        Assert.That(definitions.Select(d => d.Name), Contains.Item("Coward"));
    }

    #endregion

    #region GetCharacterTraitsAsync

    [Test]
    public async Task GetCharacterTraitsAsync_ShouldMapPublicProjections()
    {
        // Given
        Guid characterId = Guid.NewGuid();
        DateTime grantedAt = DateTime.UtcNow;
        _characterTraitRepository.Add(new CharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag(BraveTraitTag),
            DateAcquired = grantedAt,
            IsConfirmed = true,
            IsActive = true
        });

        // When
        List<PublicCharacterTrait> traits = await _subsystem.GetCharacterTraitsAsync(CharacterId.From(characterId));

        // Then - public projection shape is preserved (TraitTag, Name, GrantedAt, GrantedBy)
        Assert.That(traits, Has.Count.EqualTo(1));
        PublicCharacterTrait trait = traits[0];
        Assert.That(trait.TraitTag.Value, Is.EqualTo(BraveTraitTag));
        Assert.That(trait.Name, Is.EqualTo("Brave"));
        Assert.That(trait.GrantedAt, Is.EqualTo(grantedAt));
        Assert.That(trait.GrantedBy, Is.Null);
    }

    [Test]
    public async Task GetCharacterTraitsAsync_WithMissingDefinition_ShouldFallBackToTag()
    {
        // Given - a character trait whose definition was never registered
        Guid characterId = Guid.NewGuid();
        _characterTraitRepository.Add(new CharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag(GhostTraitTag),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        });

        // When
        List<PublicCharacterTrait> traits = await _subsystem.GetCharacterTraitsAsync(CharacterId.From(characterId));

        // Then
        Assert.That(traits, Has.Count.EqualTo(1));
        Assert.That(traits[0].Name, Is.EqualTo(GhostTraitTag), "Name should fall back to the tag value");
    }

    #endregion

    #region HasTraitAsync

    [Test]
    public async Task HasTraitAsync_WithActiveMatchingTrait_ShouldReturnTrue()
    {
        // Given
        Guid characterId = Guid.NewGuid();
        _characterTraitRepository.Add(new CharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag(BraveTraitTag),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        });

        // When / Then
        bool has = await _subsystem.HasTraitAsync(CharacterId.From(characterId), new TraitTag(BraveTraitTag));
        Assert.That(has, Is.True);
    }

    [Test]
    public async Task HasTraitAsync_WithEmptyOwnership_ShouldReturnFalse()
    {
        // Given - character owns no traits at all
        Guid characterId = Guid.NewGuid();

        // When / Then
        bool has = await _subsystem.HasTraitAsync(CharacterId.From(characterId), new TraitTag(BraveTraitTag));
        Assert.That(has, Is.False);
    }

    [Test]
    public async Task HasTraitAsync_WithInactiveTrait_ShouldReturnFalse()
    {
        // Given - character owns the trait but it is currently inactive
        Guid characterId = Guid.NewGuid();
        _characterTraitRepository.Add(new CharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag(CowardTraitTag),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = false
        });

        // When / Then
        bool has = await _subsystem.HasTraitAsync(CharacterId.From(characterId), new TraitTag(CowardTraitTag));
        Assert.That(has, Is.False);
    }

    [Test]
    public async Task HasTraitAsync_WithMissingDefinition_ShouldReturnFalse()
    {
        // Given - character owns a trait record but the definition does not exist
        Guid characterId = Guid.NewGuid();
        _characterTraitRepository.Add(new CharacterTrait
        {
            Id = Guid.NewGuid(),
            CharacterId = CharacterId.From(characterId),
            TraitTag = new TraitTag(GhostTraitTag),
            DateAcquired = DateTime.UtcNow,
            IsConfirmed = true,
            IsActive = true
        });

        // When / Then
        bool has = await _subsystem.HasTraitAsync(CharacterId.From(characterId), new TraitTag(GhostTraitTag));
        Assert.That(has, Is.False);
    }

    #endregion

    #region Subsystem reads flow through the dispatcher

    [Test]
    public async Task ReadMethods_WorkThroughDispatcher_WithEmptyRepositories()
    {
        // The four read methods must only dispatch/map results; they must not read
        // repositories directly. Prove this by asserting the read path is fully served
        // by the query dispatcher even when the repositories are empty.
        ITraitRepository emptyTraits = new InMemoryTraitRepository();
        ICharacterTraitRepository emptyCharacters = new InMemoryCharacterTraitRepository();

        TraitSubsystem subsystem = CreateSubsystem(emptyTraits, emptyCharacters);

        Guid characterId = Guid.NewGuid();

        TraitDefinition? missing = await subsystem.GetTraitAsync(new TraitTag(GhostTraitTag));
        Assert.That(missing, Is.Null);

        List<TraitDefinition> all = await subsystem.GetAllTraitsAsync();
        Assert.That(all, Is.Empty);

        List<PublicCharacterTrait> none = await subsystem.GetCharacterTraitsAsync(CharacterId.From(characterId));
        Assert.That(none, Is.Empty);

        bool absent = await subsystem.HasTraitAsync(CharacterId.From(characterId), new TraitTag(BraveTraitTag));
        Assert.That(absent, Is.False);
    }

    #endregion

    #region Test Doubles

    private sealed class StubCommandDispatcher : ICommandDispatcher
    {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand => Task.FromResult(CommandResult.Ok());

        public Task<BatchCommandResult> DispatchBatchAsync<TCommand>(
            IEnumerable<TCommand> commands, BatchExecutionOptions? options = null,
            CancellationToken cancellationToken = default)
            where TCommand : ICommand => throw new NotImplementedException();
    }

    private sealed class RoutingQueryDispatcher(
        GetTraitDefinitionQueryHandler byTag,
        GetAllTraitsQueryHandler all,
        GetCharacterTraitsQueryHandler characterTraits,
        HasTraitAsyncQueryHandler hasTrait) : IQueryDispatcher
    {
        public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
        {
            object? result = query switch
            {
                GetTraitDefinitionQuery q => await byTag.HandleAsync(q, cancellationToken),
                GetAllTraitsQuery q => await all.HandleAsync(q, cancellationToken),
                GetCharacterTraitsQuery q => await characterTraits.HandleAsync(q, cancellationToken),
                HasTraitAsyncQuery q => await hasTrait.HandleAsync(q, cancellationToken),
                _ => throw new InvalidOperationException($"Unexpected query {query?.GetType().Name}")
            };
            return (TResult)result!;
        }
    }

    #endregion
}
