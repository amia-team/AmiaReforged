using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Systems.WorldEngine.Models;

namespace AmiaReforged.EngineSpecs;

[Binding]
public class CharacterSteps
{
    [Given("a Player with the Public CD Key {string}")]
    public void GivenAPlayerWithThePublicCdKey(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Player finishes Character creation")]
    public void WhenThePlayerFinishesCharacterCreation()
    {
        ScenarioContext.StepIsPending();
    }

    [Given("there is already a Character with the GUID {string}")]
    public void GivenThereIsAlreadyACharacterWithTheGuid(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Player is playing the Character with the GUID {string}")]
    public void GivenThePlayerIsPlayingTheCharacterWithTheGuid(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("their Character should be retrieved for use in the system")]
    public void ThenTheirCharacterShouldBeRetrievedForUseInTheSystem()
    {
        ScenarioContext.StepIsPending();
    }

    [Then("their character should have a valid GUID")]
    public void ThenTheirCharacterShouldHaveAValidGuid()
    {
        ScenarioContext.StepIsPending();
    }

    [Given("a Dungeon Master with the Key {string}")]
    public void GivenADungeonMasterWithTheKey(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Dungeon Master creates a Character named {string}")]
    public void WhenTheDungeonMasterCreatesACharacterNamed(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character should be owned by the Dungeon Master with the Key {string}")]
    public void ThenTheCharacterShouldBeOwnedByTheDungeonMasterWithTheKey(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the System with the tag {string}")]
    public void GivenTheSystemWithTheTag(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [When("a System Character named {string} is created with the tag {string}")]
    public void WhenASystemCharacterNamedIsCreatedWithTheTag(string p0, string p1)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character should be owned by the System with the tag {string}")]
    public void ThenTheCharacterShouldBeOwnedByTheSystemWithTheTag(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Character is owned by the System with the tag {string}")]
    public void GivenTheCharacterIsOwnedByTheSystemWithTheTag(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Character is assigned to the Player with the Public CD Key {string}")]
    public void WhenTheCharacterIsAssignedToThePlayerWithThePublicCdKey(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character should be owned by the Player with the Public CD Key {string}")]
    public void ThenTheCharacterShouldBeOwnedByThePlayerWithThePublicCdKey(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Character is owned by the Player with the Public CD Key {string}")]
    public void GivenTheCharacterIsOwnedByThePlayerWithThePublicCdKey(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Character is assigned to the Dungeon Master with the Key {string}")]
    public void WhenTheCharacterIsAssignedToTheDungeonMasterWithTheKey(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character should not be owned by any Player")]
    public void ThenTheCharacterShouldNotBeOwnedByAnyPlayer()
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character should not be owned by the System")]
    public void ThenTheCharacterShouldNotBeOwnedByTheSystem()
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Character's name is {string}")]
    public void GivenTheCharactersNameIs(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Character is renamed to {string}")]
    public void WhenTheCharacterIsRenamedTo(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character's name should be {string}")]
    public void ThenTheCharactersNameShouldBe(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Character is active")]
    public void GivenTheCharacterIsActive()
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Character is deactivated")]
    public void WhenTheCharacterIsDeactivated()
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character should be marked inactive")]
    public void ThenTheCharacterShouldBeMarkedInactive()
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Player has Characters named:")]
    public void GivenThePlayerHasCharactersNamed(Reqnroll.Table table)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Player requests their Character list")]
    public void WhenThePlayerRequestsTheirCharacterList()
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Player should receive {int} Characters")]
    public void ThenThePlayerShouldReceiveCharacters(int p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the list should contain a Character named {string}")]
    public void ThenTheListShouldContainACharacterNamed(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Player requests the Character with the GUID {string}")]
    public void WhenThePlayerRequestsTheCharacterWithTheGuid(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("no Character exists with the GUID {string}")]
    public void GivenNoCharacterExistsWithTheGuid(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the request should fail because the Character was not found")]
    public void ThenTheRequestShouldFailBecauseTheCharacterWasNotFound()
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Player with the Public CD Key {string} tries to play the Character with the GUID {string}")]
    public void WhenThePlayerWithThePublicCdKeyTriesToPlayTheCharacterWithTheGuid(string p0, string p1)
    {
        ScenarioContext.StepIsPending();
    }

    [When("the Player creates a Character named {string}")]
    public void WhenThePlayerCreatesACharacterNamed(string first)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("both Characters should have valid GUIDs")]
    public void ThenBothCharactersShouldHaveValidGuiDs()
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the GUIDs should be unique")]
    public void ThenTheGuiDsShouldBeUnique()
    {
        ScenarioContext.StepIsPending();
    }

    [Given("a System Character named {string} is created with the tag {string}")]
    public void GivenASystemCharacterNamedIsCreatedWithTheTag(string p0, string p1)
    {
        ScenarioContext.StepIsPending();
    }

    [Given("the Character is recorded with the GUID {string}")]
    public void GivenTheCharacterIsRecordedWithTheGuid(string p0)
    {
        ScenarioContext.StepIsPending();
    }

    [Then("the Character's GUID should still be {string}")]
    public void ThenTheCharactersGuidShouldStillBe(string p0)
    {
        ScenarioContext.StepIsPending();
    }
}

public sealed class InMemoryCharacterRepository : ICharacterRepository
{
    private readonly Dictionary<Guid, Character> _byId = new();
    private readonly Dictionary<string, HashSet<Guid>> _byOwner = new(StringComparer.OrdinalIgnoreCase);

    public Task<Character?> GetByGuidAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_byId.TryGetValue(id, out Character? c) ? c : null);


    public Task AddAsync(Character character, CancellationToken ct = default)
    {
        _byId[character.Id] = character;
        Index(character);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<Character>> GetByOwnerAsync(CharacterOwner owner, CancellationToken ct = default)
    {
        string key = NormalizeOwner(owner);
        return Task.FromResult<IReadOnlyList<Character>>(_byOwner.TryGetValue(key, out HashSet<Guid>? ids)
            ? ids.Select(id => _byId[id]).ToList()
            : []);
    }

    private void Index(Character c)
    {
        string key = NormalizeOwner(c.Owner);
        if (!_byOwner.TryGetValue(key, out HashSet<Guid>? set))
        {
            set = [];
            _byOwner[key] = set;
        }

        set.Add(c.Id);
    }

    private static string NormalizeOwner(CharacterOwner owner) => owner switch
    {
        CharacterOwner.Player p => $"player:{p.Key}",
        CharacterOwner.DungeonMaster d => $"dm:{d.Key}",
        CharacterOwner.System s => $"system:{s.Key}",
        _ => "unknown"
    };
}
