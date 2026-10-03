using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;
using NUnit.Framework;
using CharacterTrait = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.CharacterTrait;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture]
public class PlayerCodexTraitsTests
{
    private InMemoryCharacterTraitRepository _ownership = null!;
    private InMemoryTraitRepository _definitions = null!;
    private CodexQueryService _queries = null!;
    private CharacterId _character;

    [SetUp]
    public void SetUp()
    {
        _character = CharacterId.New();
        _ownership = new();
        _definitions = new();
        _definitions.Add(new Trait
        {
            Tag = "brave", Name = "Brave", Description = "You stand firm in the face of danger.",
            Category = TraitCategory.Personality, PointCost = 1
        });
        _queries = new CodexQueryService(new QueryDispatcher(
            [new GetPlayerCodexTraitsHandler(_ownership, _definitions)]));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ExistingBrave_IsVisibleWithoutAnyCodexJournal(bool confirmed)
    {
        CharacterTrait owned = Own("brave", confirmed: confirmed);
        var entries = await _queries.GetPlayerTraitsAsync(_character, null);
        Assert.That(entries, Has.Count.EqualTo(1));
        PlayerTraitDisplayItem display = new(entries[0]);
        Assert.That(display.DisplayName, Is.EqualTo("Brave"));
        Assert.That(display.DetailTitle, Is.EqualTo("Brave"));
        Assert.That(display.DetailBody, Does.Contain("Personality").And.Contain("2026-01-02")
            .And.Contain("You stand firm in the face of danger."));
        Assert.That(entries[0].IsConfirmed, Is.EqualTo(confirmed));
        Assert.That(_ownership.GetByCharacterId(_character).Single(), Is.SameAs(owned));
        Assert.That(owned.IsConfirmed, Is.EqualTo(confirmed), "Reading must not confirm a selection.");
    }

    [TestCase(null, 1)]
    [TestCase(TraitCategory.Personality, 1)]
    [TestCase(TraitCategory.Background, 0)]
    public async Task CategoryFilter_UsesTheOwnedTraitsLiveDefinition(TraitCategory? category, int count)
    {
        Own("brave");
        var entries = await _queries.GetPlayerTraitsAsync(_character, category);
        Assert.That(entries, Has.Count.EqualTo(count));
    }

    [Test]
    public async Task SelectionConfirmationAndDeselection_AreReflectedOnReload()
    {
        TraitSelectionService selection = new(_ownership, _definitions);
        Assert.That(selection.SelectTrait(_character.Value, "brave", TestCharacterInfo.From("human"), []), Is.True);
        var pending = await _queries.GetPlayerTraitsAsync(_character, null);
        Assert.That(pending, Has.Count.EqualTo(1));
        Assert.That(new PlayerTraitDisplayItem(pending[0]).Subtitle, Does.Contain("Unconfirmed"));

        Assert.That(selection.DeselectTrait(_character.Value, "brave"), Is.True);
        Assert.That(await _queries.GetPlayerTraitsAsync(_character, null), Is.Empty);
        Assert.That(selection.SelectTrait(_character.Value, "brave", TestCharacterInfo.From("human"), []), Is.True);

        Assert.That(selection.ConfirmTraits(_character.Value), Is.True);
        var confirmed = await _queries.GetPlayerTraitsAsync(_character, null);
        Assert.That(confirmed.Single().IsConfirmed, Is.True);
        Assert.That(new PlayerTraitDisplayItem(confirmed[0]).Subtitle, Is.EqualTo("Personality"));

        Assert.That(selection.DeselectTrait(_character.Value, "brave"), Is.False);
        Assert.That(await _queries.GetPlayerTraitsAsync(_character, null), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GrantedDmOnlyTrait_IsVisibleToItsOwner_AndRemovalDisappears()
    {
        _definitions.Add(new Trait
        {
            Tag = "blessed", Name = "Blessed", Description = "A divine blessing.",
            Category = TraitCategory.Blessing, PointCost = 0, DmOnly = true
        });
        GrantTraitCommandHandler grant = new(_definitions, _ownership);
        var result = await grant.HandleAsync(new GrantTraitCommand(_character, new TraitTag("blessed")));
        Assert.That(result.Success, Is.True);
        Assert.That((await _queries.GetPlayerTraitsAsync(_character, TraitCategory.Blessing)).Single().Name,
            Is.EqualTo("Blessed"));

        RemoveTraitCommandHandler remove = new(_ownership);
        Assert.That((await remove.HandleAsync(new RemoveTraitCommand(_character, new TraitTag("blessed")))).Success, Is.True);
        Assert.That(await _queries.GetPlayerTraitsAsync(_character, null), Is.Empty);
    }

    [Test]
    public async Task OtherCharactersAndUnownedDefinitions_DoNotAppear()
    {
        _ownership.Add(new CharacterTrait
        {
            Id = Guid.NewGuid(), CharacterId = CharacterId.New(), TraitTag = new TraitTag("brave")
        });
        Assert.That(await _queries.GetPlayerTraitsAsync(_character, null), Is.Empty);
    }

    [Test]
    public async Task InactiveTrait_RemainsListedWithItsActualStatus()
    {
        CharacterTrait owned = Own("brave");
        owned.IsActive = false;
        var entries = await _queries.GetPlayerTraitsAsync(_character, null);
        PlayerTraitDisplayItem display = new(entries.Single());
        Assert.That(display.Subtitle, Does.Contain("Inactive"));
        Assert.That(display.DetailBody, Does.Contain("Status: Inactive / Confirmed"));
    }

    [Test]
    public async Task MissingDefinition_FallsBackToOwnedTagInsteadOfDroppingTrait()
    {
        Own("unknown_trait");
        var entries = await _queries.GetPlayerTraitsAsync(_character, null);
        Assert.That(entries.Single().Name, Is.EqualTo("unknown_trait"));
        Assert.That(new PlayerTraitDisplayItem(entries[0]).Subtitle, Is.EqualTo("Uncategorized"));
        Assert.That(await _queries.GetPlayerTraitsAsync(_character, TraitCategory.Background), Is.Empty);
    }

    private CharacterTrait Own(string tag, bool confirmed = true)
    {
        CharacterTrait trait = new()
        {
            Id = Guid.NewGuid(), CharacterId = _character, TraitTag = new TraitTag(tag),
            DateAcquired = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc), IsConfirmed = confirmed
        };
        _ownership.Add(trait);
        return trait;
    }
}
