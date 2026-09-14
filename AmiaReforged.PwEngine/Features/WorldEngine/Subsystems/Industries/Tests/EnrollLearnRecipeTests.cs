using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Tests;

[TestFixture]
public class EnrollLearnRecipeTests
{
    private InMemoryIndustryRepository _industryRepository = null!;
    private InMemoryIndustryMembershipRepository _membershipRepository = null!;
    private InMemoryCharacterKnowledgeRepository _knowledgeRepository = null!;
    private InMemoryEventBus _eventBus = null!;
    private StubCharacterRepository _characterRepository = null!;
    private Industry _testIndustry = null!;
    private Recipe _testRecipe = null!;
    private CharacterId _testCharacterId;

    [SetUp]
    public void SetUp()
    {
        _industryRepository = new InMemoryIndustryRepository();
        _membershipRepository = new InMemoryIndustryMembershipRepository();
        _knowledgeRepository = new InMemoryCharacterKnowledgeRepository();
        _eventBus = new InMemoryEventBus();
        _characterRepository = new StubCharacterRepository();

        _testCharacterId = CharacterId.From(Guid.NewGuid());
        _characterRepository.AddExisting(_testCharacterId.Value);

        _testRecipe = new Recipe
        {
            RecipeId = new RecipeId("iron_sword"),
            Name = "Iron Sword",
            IndustryTag = new IndustryTag("blacksmithing"),
            RequiredKnowledge = ["basic_forging"],
            Ingredients =
            [
                new Ingredient
                {
                    ItemTag = "iron_ingot",
                    Quantity = Quantity.Parse(3),
                    MinQuality = 1
                }
            ],
            Products =
            [
                new Product
                {
                    ItemTag = "iron_sword",
                    Quantity = Quantity.Parse(1),
                    Quality = 2
                }
            ],
            ProgressionPointsAwarded = 5
        };

        _testIndustry = new Industry
        {
            Tag = "blacksmithing",
            Name = "Blacksmithing",
            Knowledge =
            [
                new Knowledge
                {
                    Tag = "basic_forging",
                    Name = "Basic Forging",
                    Description = "Basic forging techniques",
                    Level = ProficiencyLevel.Novice,
                    PointCost = 1
                }
            ],
            Recipes = [_testRecipe]
        };
        _industryRepository.Add(_testIndustry);
    }

    private EnrollInIndustryHandler CreateEnrollHandler() =>
        new(_industryRepository, _membershipRepository, _characterRepository, _eventBus);

    private LearnRecipeHandler CreateLearnHandler() =>
        new(_industryRepository, _membershipRepository, _knowledgeRepository,
            new RecipeTemplateExpander(
                new EmptyRecipeTemplateRepository(),
                new EmptyItemDefinitionRepository()));

    private void EnrollTestCharacter()
    {
        _membershipRepository.Add(new IndustryMembership
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            Level = ProficiencyLevel.Layman,
            CharacterKnowledge = []
        });
    }

    private void TeachKnowledge(string tag)
    {
        Knowledge? definition = _testIndustry.Knowledge.FirstOrDefault(k => k.Tag == tag);
        Assert.That(definition, Is.Not.Null);
        _knowledgeRepository.Add(new CharacterKnowledge
        {
            Id = Guid.NewGuid(),
            IndustryTag = "blacksmithing",
            Definition = definition!,
            CharacterId = _testCharacterId.Value
        });
    }

    [Test]
    public async Task Enroll_Success_PersistsAndPublishesEvent()
    {
        CommandResult result = await CreateEnrollHandler().HandleAsync(new EnrollInIndustryCommand
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        });

        Assert.That(result.Success, Is.True);
        Assert.That(_membershipRepository.All(_testCharacterId.Value), Has.Count.EqualTo(1));
        Assert.That(_eventBus.PublishedEvents.OfType<MemberJoinedIndustryEvent>().SingleOrDefault(), Is.Not.Null);
    }

    [Test]
    public async Task Enroll_UnknownIndustry_Fails()
    {
        CommandResult result = await CreateEnrollHandler().HandleAsync(new EnrollInIndustryCommand
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("nonexistent")
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task Enroll_UnknownCharacter_Fails()
    {
        CommandResult result = await CreateEnrollHandler().HandleAsync(new EnrollInIndustryCommand
        {
            CharacterId = CharacterId.From(Guid.NewGuid()),
            IndustryTag = new IndustryTag("blacksmithing")
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task Enroll_Duplicate_Fails()
    {
        EnrollInIndustryHandler handler = CreateEnrollHandler();
        EnrollInIndustryCommand command = new()
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        };

        Assert.That((await handler.HandleAsync(command)).Success, Is.True);
        CommandResult second = await handler.HandleAsync(command);

        Assert.That(second.Success, Is.False);
        Assert.That(second.ErrorMessage, Does.Contain("Already enrolled"));
    }

    [Test]
    public async Task LearnRecipe_Success_WhenMemberWithPrereqs()
    {
        EnrollTestCharacter();
        TeachKnowledge("basic_forging");

        CommandResult result = await CreateLearnHandler().HandleAsync(new LearnRecipeCommand
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = "iron_sword"
        });

        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task LearnRecipe_NotEnrolled_Fails()
    {
        TeachKnowledge("basic_forging");

        CommandResult result = await CreateLearnHandler().HandleAsync(new LearnRecipeCommand
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = "iron_sword"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not a member"));
    }

    [Test]
    public async Task LearnRecipe_UnknownRecipe_Fails()
    {
        EnrollTestCharacter();

        CommandResult result = await CreateLearnHandler().HandleAsync(new LearnRecipeCommand
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = "nonexistent_recipe"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task LearnRecipe_MissingPrereqs_Fails()
    {
        EnrollTestCharacter();

        CommandResult result = await CreateLearnHandler().HandleAsync(new LearnRecipeCommand
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = "iron_sword"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("prerequisite"));
    }

    [Test]
    public async Task GetRecipe_ReturnsManualRecipe_AndNullWhenMissing()
    {
        GetRecipeHandler handler = new(_industryRepository,
            new RecipeTemplateExpander(
                new EmptyRecipeTemplateRepository(),
                new EmptyItemDefinitionRepository()));

        Recipe? found = await handler.HandleAsync(new GetRecipeQuery
        {
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = "iron_sword"
        });
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Name, Is.EqualTo("Iron Sword"));

        Recipe? missing = await handler.HandleAsync(new GetRecipeQuery
        {
            IndustryTag = new IndustryTag("blacksmithing"),
            RecipeId = "nonexistent_recipe"
        });
        Assert.That(missing, Is.Null);

        Recipe? noIndustry = await handler.HandleAsync(new GetRecipeQuery
        {
            IndustryTag = new IndustryTag("nonexistent"),
            RecipeId = "iron_sword"
        });
        Assert.That(noIndustry, Is.Null);
    }

    [Test]
    public async Task MembershipQueries_RoundTrip()
    {
        EnrollTestCharacter();
        TeachKnowledge("basic_forging");

        GetMembershipHandler membershipHandler = new(_membershipRepository);
        IndustryMembership? membership = await membershipHandler.HandleAsync(new GetMembershipQuery
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        });
        Assert.That(membership, Is.Not.Null);

        IndustryMembership? missing = await membershipHandler.HandleAsync(new GetMembershipQuery
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("alchemy")
        });
        Assert.That(missing, Is.Null);

        GetCharacterIndustriesHandler industriesHandler = new(_membershipRepository);
        List<IndustryMembership> all = await industriesHandler.HandleAsync(new GetCharacterIndustriesQuery
        {
            CharacterId = _testCharacterId
        });
        Assert.That(all, Has.Count.EqualTo(1));

        GetKnownRecipesHandler knownHandler = new(_knowledgeRepository);
        List<string> known = await knownHandler.HandleAsync(new GetKnownRecipesQuery
        {
            CharacterId = _testCharacterId,
            IndustryTag = new IndustryTag("blacksmithing")
        });
        Assert.That(known, Does.Contain("basic_forging"));
    }

    private sealed class StubCharacterRepository : ICharacterRepository
    {
        private readonly HashSet<Guid> _existing = [];

        public void AddExisting(Guid id) => _existing.Add(id);

        public void Add(ICharacter character) { }
        public bool Exists(Guid characterId) => _existing.Contains(characterId);
        public void Delete(ICharacter character) { }
        public void DeleteById(Guid characterId) => _existing.Remove(characterId);
        public ICharacter? GetById(Guid characterId) => null;
    }
}
