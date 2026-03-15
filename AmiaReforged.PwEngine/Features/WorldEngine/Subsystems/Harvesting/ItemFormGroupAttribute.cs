namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

/// <summary>
/// Attribute applied to <see cref="ItemForm"/> members to declare their <see cref="ItemFormGroup"/>.
/// Follows the same pattern as <see cref="Items.ItemData.MaterialCategoryAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ItemFormGroupAttribute : Attribute
{
    public ItemFormGroup Group { get; }

    public ItemFormGroupAttribute(ItemFormGroup group)
    {
        Group = group;
    }
}
