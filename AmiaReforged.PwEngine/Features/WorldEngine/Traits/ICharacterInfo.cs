namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits;

/// <summary>
/// Interface providing character information needed for trait eligibility checks.
/// Adapters at the NWN boundary will implement this for actual game characters.
/// </summary>
public interface ICharacterInfo
{
    string Race { get; }
    List<string> Classes { get; }
}
