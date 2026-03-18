using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;

/// <summary>
/// Returns all trait tags for the current character.
/// Reads from the "character_traits" variable populated by the trait hook service.
/// </summary>
public class GetCreatureTraitsExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "trait.get_creature_traits";
    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node, GlyphExecutionContext context, Func<string, Task<object?>> resolveInput)
    {
        List<string> traits = [];
        if (context.Variables.TryGetValue("character_traits", out object? traitsObj)
            && traitsObj is List<string> traitList)
        {
            traits = traitList;
        }

        return Task.FromResult(GlyphNodeResult.Data(new Dictionary<string, object?>
        {
            ["traits"] = traits,
            ["count"] = traits.Count
        }));
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Get Creature Traits",
        Category = "Traits",
        Description = "Returns a list of all trait tags the character currently has, and their count.",
        ColorClass = "node-getter",
        ScriptCategory = GlyphScriptCategory.Trait,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "traits", Name = "Trait Tags", DataType = GlyphDataType.List, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "count", Name = "Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output }
        ]
    };
}
