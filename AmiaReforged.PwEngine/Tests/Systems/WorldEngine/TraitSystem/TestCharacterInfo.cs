using AmiaReforged.PwEngine.Features.WorldEngine.Traits;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.TraitSystem;

/// <summary>
/// Test stub implementing ICharacterInfo for trait eligibility testing.
/// </summary>
public class TestCharacterInfo : ICharacterInfo
{
    public required string Race { get; init; }
    public required List<string> Classes { get; init; }
}
