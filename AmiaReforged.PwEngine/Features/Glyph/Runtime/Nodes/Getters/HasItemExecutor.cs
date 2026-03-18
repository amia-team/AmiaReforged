using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Pure function node that checks if a creature possesses an item with a given tag.
/// Returns whether the item exists and how many matching items are found.
/// </summary>
public class HasItemExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.has_item";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput("creature");
        object? itemTagValue = await resolveInput("item_tag");

        uint creature = Convert.ToUInt32(creatureValue);
        string itemTag = itemTagValue?.ToString() ?? string.Empty;

        bool hasItem = false;
        int count = 0;

        if (creature != NWScript.OBJECT_INVALID && !string.IsNullOrEmpty(itemTag))
        {
            // Iterate inventory looking for items with the matching tag
            uint item = NWScript.GetFirstItemInInventory(creature);
            while (item != NWScript.OBJECT_INVALID)
            {
                if (string.Equals(NWScript.GetTag(item), itemTag, StringComparison.OrdinalIgnoreCase))
                {
                    hasItem = true;
                    count += NWScript.GetItemStackSize(item);
                }
                item = NWScript.GetNextItemInInventory(creature);
            }
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["has_item"] = hasItem,
            ["count"] = count
        });
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Has Item",
        Category = "Getters",
        Description = "Checks if a creature has an item with the specified tag in their inventory. " +
                      "Returns whether any matching item exists and the total stack count.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "item_tag", Name = "Item Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "has_item", Name = "Has Item", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "count", Name = "Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
