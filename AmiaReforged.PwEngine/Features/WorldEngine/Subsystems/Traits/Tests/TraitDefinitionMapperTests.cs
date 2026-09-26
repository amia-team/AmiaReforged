using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

/// <summary>
/// Serialization contract tests for <see cref="TraitDefinitionMapper"/>.
/// These tests exercise only the mapper's JSON round-tripping. They do not
/// instantiate EF Core, PostgreSQL, or any persistence repository.
/// </summary>
[TestFixture]
public class TraitDefinitionMapperTests
{
    private TraitDefinitionMapper _mapper = default!;

    [SetUp]
    public void SetUp()
    {
        _mapper = new TraitDefinitionMapper();
    }

    [Test]
    public void RoundTrip_PreservesTraitDefinitionData()
    {
        // Arrange: a trait carrying non-default values for every mapped field.
        var trait = new Trait
        {
            Tag = "hero",
            Name = "Hero",
            Description = "A brave champion of the realm.",
            PointCost = 5,
            Category = TraitCategory.Physical,
            DeathBehavior = TraitDeathBehavior.Permadeath,
            RequiresUnlock = true,
            DmOnly = false,
            Effects =
            [
                TraitEffect.SkillModifier("Athletics", 2),
                TraitEffect.AttributeModifier("Strength", -1)
            ],
            AllowedRaces = ["Human", "Elf"],
            AllowedClasses = ["Fighter", "Ranger"],
            ForbiddenRaces = ["Deadapawn"],
            ForbiddenClasses = ["Warlock"],
            ConflictingTraits = ["Coward"],
            PrerequisiteTraits = ["Veteran"]
        };

        // Act: domain -> persisted -> domain.
        var persisted = _mapper.ToPersistent(trait);
        var roundTripped = _mapper.ToDomain(persisted);

        // Assert: scalar fields preserved.
        Assert.That(roundTripped.Tag, Is.EqualTo(trait.Tag));
        Assert.That(roundTripped.Name, Is.EqualTo(trait.Name));
        Assert.That(roundTripped.Description, Is.EqualTo(trait.Description));
        Assert.That(roundTripped.PointCost, Is.EqualTo(trait.PointCost));
        Assert.That(roundTripped.Category, Is.EqualTo(trait.Category));
        Assert.That(roundTripped.DeathBehavior, Is.EqualTo(trait.DeathBehavior));
        Assert.That(roundTripped.RequiresUnlock, Is.EqualTo(trait.RequiresUnlock));
        Assert.That(roundTripped.DmOnly, Is.EqualTo(trait.DmOnly));

        // Assert: effects preserved (count + per-effect values).
        Assert.That(roundTripped.Effects, Has.Count.EqualTo(2));
        Assert.That(roundTripped.Effects[0].EffectType, Is.EqualTo(TraitEffectType.SkillModifier));
        Assert.That(roundTripped.Effects[0].Target, Is.EqualTo("Athletics"));
        Assert.That(roundTripped.Effects[0].Magnitude, Is.EqualTo(2));
        Assert.That(roundTripped.Effects[0].Description, Is.EqualTo(trait.Effects[0].Description));

        Assert.That(roundTripped.Effects[1].EffectType, Is.EqualTo(TraitEffectType.AttributeModifier));
        Assert.That(roundTripped.Effects[1].Target, Is.EqualTo("Strength"));
        Assert.That(roundTripped.Effects[1].Magnitude, Is.EqualTo(-1));
        Assert.That(roundTripped.Effects[1].Description, Is.EqualTo(trait.Effects[1].Description));

        // Assert: race/class / relationship collections preserved.
        Assert.That(roundTripped.AllowedRaces, Is.EqualTo(trait.AllowedRaces));
        Assert.That(roundTripped.AllowedClasses, Is.EqualTo(trait.AllowedClasses));
        Assert.That(roundTripped.ForbiddenRaces, Is.EqualTo(trait.ForbiddenRaces));
        Assert.That(roundTripped.ForbiddenClasses, Is.EqualTo(trait.ForbiddenClasses));
        Assert.That(roundTripped.ConflictingTraits, Is.EqualTo(trait.ConflictingTraits));
        Assert.That(roundTripped.PrerequisiteTraits, Is.EqualTo(trait.PrerequisiteTraits));
    }

    [Test]
    public void ToDomain_WhenJsonCollectionsAreEmpty_ReturnsEmptyCollections()
    {
        // Arrange: every JSON collection field is an explicit empty array.
        var persisted = new PersistedTraitDefinition
        {
            Tag = "empty",
            Name = "Empty",
            Description = "No collections.",
            EffectsJson = "[]",
            AllowedRacesJson = "[]",
            AllowedClassesJson = "[]",
            ForbiddenRacesJson = "[]",
            ForbiddenClassesJson = "[]",
            ConflictingTraitsJson = "[]",
            PrerequisiteTraitsJson = "[]"
        };

        // Act.
        var trait = _mapper.ToDomain(persisted);

        // Assert.
        Assert.That(trait.Effects, Is.Empty);
        Assert.That(trait.AllowedRaces, Is.Empty);
        Assert.That(trait.AllowedClasses, Is.Empty);
        Assert.That(trait.ForbiddenRaces, Is.Empty);
        Assert.That(trait.ForbiddenClasses, Is.Empty);
        Assert.That(trait.ConflictingTraits, Is.Empty);
        Assert.That(trait.PrerequisiteTraits, Is.Empty);
    }

    [Test]
    public void ToDomain_WhenJsonCollectionsAreWhitespace_ReturnsEmptyCollections()
    {
        // Arrange: every JSON collection field is whitespace.
        var persisted = new PersistedTraitDefinition
        {
            Tag = "ws",
            Name = "Whitespace",
            Description = "Whitespace collections.",
            EffectsJson = "   ",
            AllowedRacesJson = "\t",
            AllowedClassesJson = "  ",
            ForbiddenRacesJson = "\n",
            ForbiddenClassesJson = " \t ",
            ConflictingTraitsJson = "   ",
            PrerequisiteTraitsJson = "\r\n"
        };

        // Act.
        var trait = _mapper.ToDomain(persisted);

        // Assert.
        Assert.That(trait.Effects, Is.Empty);
        Assert.That(trait.AllowedRaces, Is.Empty);
        Assert.That(trait.AllowedClasses, Is.Empty);
        Assert.That(trait.ForbiddenRaces, Is.Empty);
        Assert.That(trait.ForbiddenClasses, Is.Empty);
        Assert.That(trait.ConflictingTraits, Is.Empty);
        Assert.That(trait.PrerequisiteTraits, Is.Empty);
    }

    [Test]
    public void ToDomain_WhenJsonIsMalformed_ReturnsEmptyCollections()
    {
        // Arrange: every JSON collection field is malformed JSON.
        var persisted = new PersistedTraitDefinition
        {
            Tag = "malformed",
            Name = "Malformed",
            Description = "Broken JSON.",
            EffectsJson = "[{ not json",
            AllowedRacesJson = "{oops",
            AllowedClassesJson = "[1, 2",
            ForbiddenRacesJson = "nullll",
            ForbiddenClassesJson = "[]]",
            ConflictingTraitsJson = "{{}",
            PrerequisiteTraitsJson = "]["
        };

        // Act & Assert: mapping must not throw even with malformed JSON.
        Assert.DoesNotThrow(() => _mapper.ToDomain(persisted));
        var trait = _mapper.ToDomain(persisted);

        // Assert: each malformed collection becomes empty.
        Assert.That(trait.Effects, Is.Empty);
        Assert.That(trait.AllowedRaces, Is.Empty);
        Assert.That(trait.AllowedClasses, Is.Empty);
        Assert.That(trait.ForbiddenRaces, Is.Empty);
        Assert.That(trait.ForbiddenClasses, Is.Empty);
        Assert.That(trait.ConflictingTraits, Is.Empty);
        Assert.That(trait.PrerequisiteTraits, Is.Empty);
    }

    [Test]
    public void ToDomain_WhenEffectTypeIsUnknown_MapsEffectTypeToNone()
    {
        // Arrange: a single effect with an undefined numeric EffectType (999).
        var effect = new
        {
            EffectType = 999,
            Target = "Athletics",
            Magnitude = 3,
            Description = "Unknown effect"
        };
        string effectsJson = JsonSerializer.Serialize(new[] { effect });

        var persisted = new PersistedTraitDefinition
        {
            Tag = "unknown",
            Name = "Unknown Effect",
            Description = "Unknown effect type.",
            EffectsJson = effectsJson
        };

        // Act.
        var trait = _mapper.ToDomain(persisted);

        // Assert.
        Assert.That(trait.Effects, Has.Count.EqualTo(1));
        Assert.That(trait.Effects[0].EffectType, Is.EqualTo(TraitEffectType.None));
        Assert.That(trait.Effects[0].Target, Is.EqualTo("Athletics"));
        Assert.That(trait.Effects[0].Magnitude, Is.EqualTo(3));
        Assert.That(trait.Effects[0].Description, Is.EqualTo("Unknown effect"));
    }
}
