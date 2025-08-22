namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

/// <summary>
/// Port to abstract away random number generation for tests
/// </summary>
public interface IRandomPort
{
    /// <summary>
    /// Generates a random double value in the range of [0, 1).
    /// </summary>
    /// <returns>A double value greater than or equal to 0, and less than 1.</returns>
    double NextUnit();
}
