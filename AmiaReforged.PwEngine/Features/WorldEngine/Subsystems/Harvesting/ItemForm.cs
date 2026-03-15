using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;

/// <summary>
/// Classifies every item in the economy by its physical form.
/// Each member carries an <see cref="ItemFormGroupAttribute"/> that assigns it
/// to one of the manufacturing-oriented <see cref="ItemFormGroup"/> categories.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ItemForm
{
    None = 0,

    // ── Tools ────────────────────────────────────────────────────
    [ItemFormGroup(ItemFormGroup.Tool)] ToolPick = 1,
    [ItemFormGroup(ItemFormGroup.Tool)] ToolHammer = 2,
    [ItemFormGroup(ItemFormGroup.Tool)] ToolAxe = 3,
    [ItemFormGroup(ItemFormGroup.Tool)] ToolFroe = 4,
    [ItemFormGroup(ItemFormGroup.Tool)] ToolAdze = 5,
    [ItemFormGroup(ItemFormGroup.Tool)] ToolWoodChisel = 6,
    [ItemFormGroup(ItemFormGroup.Tool)] ToolMasonChisle = 7,

    // ── Raw Resources ────────────────────────────────────────────
    [ItemFormGroup(ItemFormGroup.Resource)] ResourceOre = 8,
    [ItemFormGroup(ItemFormGroup.Resource)] ResourceStone = 9,
    [ItemFormGroup(ItemFormGroup.Resource)] ResourceLog = 10,
    [ItemFormGroup(ItemFormGroup.Resource)] ResourceGem = 14,
    [ItemFormGroup(ItemFormGroup.Resource)] ResourcePlant = 16,

    // ── Intermediate Products ────────────────────────────────────
    [ItemFormGroup(ItemFormGroup.IntermediateProduct)] ResourcePlank = 11,
    [ItemFormGroup(ItemFormGroup.IntermediateProduct)] ResourceBrick = 12,
    [ItemFormGroup(ItemFormGroup.IntermediateProduct)] ResourceIngot = 13,

    // ── Finished Goods ───────────────────────────────────────────
    [ItemFormGroup(ItemFormGroup.FinishedGood)] Furniture = 17,
}
