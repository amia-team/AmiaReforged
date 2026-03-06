using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;

/// <summary>
/// Checks whether a creature/character has a specific trait.
/// Reads the trait tag from the graph variable store (populated by the trait hook).
/// </summary>
public class HasTraitExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "trait.has_trait";
    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        object? tagVal = await resolveInput("trait_tag");
        string traitTag = tagVal?.ToString() ?? string.Empty;

        // Check the graph variable store for character traits
        // The trait hook service populates "character_traits" as a List<string>
        bool hasTrait = false;
        if (context.Variables.TryGetValue("character_traits", out object? traitsObj)
            && traitsObj is List<string> traits)
        {
            hasTrait = traits.Contains(traitTag, StringComparer.OrdinalIgnoreCase);
        }

        return GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["has_trait"] = hasTrait
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Has Trait",
        Category = "Traits",
        Description = "Checks if the target character has a specific trait. Returns true/false.",
        ColorClass = "node-getter",
        InputPins =
        [
            new GlyphPin { Id = "trait_tag", Name = "Trait Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "has_trait", Name = "Has Trait", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output }
        ]
    };
}
