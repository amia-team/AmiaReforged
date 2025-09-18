namespace AmiaReforged.PwEngine.Systems.WindowingSystem.Scry.GenericWindows;

/// <summary>
///     Provides a static method to create a new instance of <see cref="GenericWindowBuilder" />.
/// </summary>
public static class GenericWindow
{
    /// <summary>
    ///     Creates a new instance of <see cref="GenericWindowBuilder" />.
    /// </summary>
    /// <returns>A new <see cref="GenericWindowBuilder" /> instance.</returns>
    public static IWindowBuilder Builder() => new GenericWindowBuilder();
}