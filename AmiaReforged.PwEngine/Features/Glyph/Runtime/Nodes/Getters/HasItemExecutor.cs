using AmiaReforged.PwEngine.Features.Glyph.Core;
using NWN.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Getters;

/// <summary>
/// Pure function node that checks if a creature possesses an item with a given tag.
/// Returns whether the item exists and how many matching items are found.
/// </summary>
[GlyphNode]
public partial class HasItemExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "getter.has_item";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? creatureValue = await resolveInput(Inputs.Creature);
        object? itemTagValue = await resolveInput(Inputs.ItemTag);

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

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("has_item", "has_item", null, CallAliases: [new("player.has_item", "has_item", "player")])
        ],
        DisplayName = "Has Item",
        Category = "Getters",
        Description = "Checks if a creature has an item with the specified tag in their inventory. " +
                      "Returns whether any matching item exists and the total stack count.",
        ColorClass = "node-getter",
        Archetype = GlyphNodeArchetype.PureFunction,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("creature", "Creature", GlyphDataType.NwObject),
            Pins.In("item_tag", "Item Tag", GlyphDataType.String)
        ],
        Results =
        [
            Pins.Out("has_item", "Has Item", GlyphDataType.Bool),
            Pins.Out("count", "Count", GlyphDataType.Int)
        ]
    };
}
