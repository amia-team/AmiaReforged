using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

/// <summary>
/// Expands <see cref="ItemBlueprint"/> definitions that have <see cref="ItemBlueprint.Variants"/>
/// into concrete <see cref="ItemBlueprint"/> instances — one per <see cref="MaterialVariant"/>.
/// <para>
/// Each expanded item inherits the template's shared properties (ResRef, ItemForm, BaseItemType, etc.)
/// but has its own material, appearance, tag (auto-derived as <c>{templateTag}_{material}</c>),
/// and name (<c>{MaterialDisplayName} {TemplateName}</c>).
/// </para>
/// <para>
/// Concrete items are cached in memory. The cache is invalidated and rebuilt when templates
/// change (call <see cref="Invalidate"/>).
/// </para>
/// </summary>
[ServiceBinding(typeof(ItemBlueprintExpander))]
public class ItemBlueprintExpander
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex PascalSplitRegex = new(@"(?<=[a-z])(?=[A-Z])", RegexOptions.Compiled);

    private readonly IItemDefinitionRepository _itemRepository;

    /// <summary>
    /// Cache: template item tag → list of expanded concrete items.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<ItemBlueprint>> _expandedByTemplate = new();

    private volatile bool _isExpanded;

    public ItemBlueprintExpander(IItemDefinitionRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    /// <summary>
    /// Expands all item blueprint templates and registers the concrete items in the repository.
    /// Safe to call multiple times — will skip if already expanded.
    /// </summary>
    public void ExpandAll()
    {
        if (_isExpanded) return;

        Log.Info("Expanding item blueprint templates...");

        List<ItemBlueprint> allItems = _itemRepository.AllItems();
        int totalExpanded = 0;

        foreach (ItemBlueprint blueprint in allItems)
        {
            if (!blueprint.IsTemplate) continue;

            List<ItemBlueprint> expanded = ExpandBlueprint(blueprint);
            _expandedByTemplate[blueprint.ItemTag] = expanded;

            // Register each expanded item in the repository so it's available
            // to the RecipeTemplateExpander and the crafting system
            foreach (ItemBlueprint item in expanded)
            {
                _itemRepository.AddItemDefinition(item);
            }

            totalExpanded += expanded.Count;
        }

        _isExpanded = true;

        Log.Info("Item blueprint expansion complete: {Count} concrete items generated from templates.",
            totalExpanded);
    }

    /// <summary>
    /// Clears the cache and triggers a full re-expansion.
    /// Call this when item blueprint templates change.
    /// </summary>
    public void Invalidate()
    {
        _isExpanded = false;
        _expandedByTemplate.Clear();
        ExpandAll();
    }

    /// <summary>
    /// Gets all expanded concrete items from a specific template.
    /// </summary>
    public List<ItemBlueprint> GetExpandedItemsForTemplate(string templateTag)
    {
        EnsureExpanded();
        return _expandedByTemplate.GetValueOrDefault(templateTag, []);
    }

    /// <summary>
    /// Gets all expanded concrete items across all templates.
    /// </summary>
    public List<ItemBlueprint> GetAllExpandedItems()
    {
        EnsureExpanded();
        return _expandedByTemplate.Values.SelectMany(list => list).ToList();
    }

    /// <summary>
    /// Expands a single blueprint template into concrete items — one per variant.
    /// </summary>
    public List<ItemBlueprint> ExpandBlueprint(ItemBlueprint template)
    {
        if (template.Variants == null || template.Variants.Count == 0)
            return [];

        List<ItemBlueprint> results = new();

        foreach (MaterialVariant variant in template.Variants)
        {
            string materialSuffix = variant.Material.ToString().ToLowerInvariant();
            string itemTag = $"{template.ItemTag}_{materialSuffix}";
            string materialDisplayName = FormatMaterialName(variant.Material);
            string name = $"{materialDisplayName} {template.Name}";

            ItemBlueprint expanded = new(
                ResRef: template.ResRef,
                ItemTag: itemTag,
                Name: name,
                Description: template.Description,
                Materials: [variant.Material],
                ItemForm: template.ItemForm,
                BaseItemType: template.BaseItemType,
                Appearance: variant.Appearance,
                LocalVariables: template.LocalVariables,
                BaseValue: variant.BaseValueOverride ?? template.BaseValue,
                WeightIncreaseConstant: template.WeightIncreaseConstant);

            results.Add(expanded);
            Log.Debug("Expanded template '{Template}' → '{Tag}' ({Name})",
                template.ItemTag, itemTag, name);
        }

        return results;
    }

    private void EnsureExpanded()
    {
        if (!_isExpanded) ExpandAll();
    }

    /// <summary>
    /// Converts a <see cref="MaterialEnum"/> name to a human-readable display name
    /// by splitting PascalCase. E.g., <c>WoodOak</c> → <c>Wood Oak</c>, <c>ColdIron</c> → <c>Cold Iron</c>.
    /// </summary>
    private static string FormatMaterialName(MaterialEnum material)
    {
        string raw = material.ToString();
        return PascalSplitRegex.Replace(raw, " ");
    }
}
