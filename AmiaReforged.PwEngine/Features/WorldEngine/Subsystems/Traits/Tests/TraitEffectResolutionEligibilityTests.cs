using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Tests;

[TestFixture]
public class TraitEffectResolutionEligibilityTests
{
    [Test]
    public void Resolution_includes_glyph_only_traits_and_excludes_inactive_unconfirmed_and_missing_definitions()
    {
        var definitions = InMemoryTraitRepository.Create();
        var characters = InMemoryCharacterTraitRepository.Create();
        Guid characterId = Guid.NewGuid();
        foreach (string tag in new[] { "glyph_only", "builtin", "inactive", "unconfirmed", "orphan" })
        {
            if (tag != "orphan")
                definitions.Add(new Trait
                {
                    Tag = tag, Name = tag, Description = tag, PointCost = 0,
                    Effects = tag == "builtin" ? [TraitEffect.SkillModifier("Hide", 2)] : []
                });
            characters.Add(new CharacterTrait
            {
                Id = Guid.NewGuid(), CharacterId = CharacterId.From(characterId), TraitTag = new TraitTag(tag),
                IsActive = tag != "inactive", IsConfirmed = tag != "unconfirmed"
            });
        }
        var service = new TraitEffectApplicationService(characters, definitions);
        Assert.That(service.GetActiveTraits(characterId).Select(t => t.Tag), Is.EquivalentTo(new[] { "glyph_only", "builtin" }));
        Assert.That(service.GetActiveEffects(characterId).Select(e => e.TraitTag), Is.EqualTo(new[] { "builtin" }));
    }
}
