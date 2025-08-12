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
