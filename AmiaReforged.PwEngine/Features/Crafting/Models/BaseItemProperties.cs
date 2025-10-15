namespace AmiaReforged.PwEngine.Features.Crafting.Models;

public class BaseItemProperties
{
    /// <summary>
    ///     The base item type that this property list is for.
    /// </summary>
    public required int BaseItem { get; init; }

    /// <summary>
    ///     A readonly collection of properties that can't be modified after creation.
    /// </summary>
    /// <returns></returns>
    public required IReadOnlyCollection<CraftingProperty> Properties { get; init; }
}
