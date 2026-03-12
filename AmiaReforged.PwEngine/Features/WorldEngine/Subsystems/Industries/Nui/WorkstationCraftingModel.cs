using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Nui;

/// <summary>
/// Model for the workstation crafting UI. Manages the recipe list, search filtering,
/// pagination, and selection state.
/// </summary>
public class WorkstationCraftingModel
{
    public const int EntriesPerPage = 8;

    private List<Recipe> _allRecipes = [];
    private List<Recipe> _filteredRecipes = [];

    public WorkstationTag WorkstationTag { get; }
    public string WorkstationName { get; }
    public string SearchFilter { get; private set; } = string.Empty;
    public int CurrentPage { get; set; }
    public int? SelectedIndex { get; set; }

    public IReadOnlyList<Recipe> FilteredRecipes => _filteredRecipes;

    public int TotalPages => Math.Max(1, (int)Math.Ceiling(_filteredRecipes.Count / (double)EntriesPerPage));

    public IReadOnlyList<Recipe> CurrentPageRecipes
    {
        get
        {
            int start = CurrentPage * EntriesPerPage;
            int count = Math.Min(EntriesPerPage, _filteredRecipes.Count - start);
            return count <= 0 ? [] : _filteredRecipes.GetRange(start, count);
        }
    }

    public WorkstationCraftingModel(WorkstationTag workstationTag, string workstationName)
    {
        WorkstationTag = workstationTag;
        WorkstationName = workstationName;
    }

    /// <summary>
    /// Loads the available recipes and resets filter/pagination state.
    /// </summary>
    public void Load(List<Recipe> recipes)
    {
        _allRecipes = recipes.OrderBy(r => r.Name).ToList();
        SearchFilter = string.Empty;
        CurrentPage = 0;
        SelectedIndex = null;
        _filteredRecipes = new List<Recipe>(_allRecipes);
    }

    /// <summary>
    /// Applies a search filter to the recipe list. Resets to page 0.
    /// </summary>
    public void ApplySearch(string term)
    {
        SearchFilter = term;
        CurrentPage = 0;
        SelectedIndex = null;

        if (string.IsNullOrWhiteSpace(term))
        {
            _filteredRecipes = new List<Recipe>(_allRecipes);
        }
        else
        {
            _filteredRecipes = _allRecipes
                .Where(r => r.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    /// <summary>
    /// Returns the recipe at the given absolute index within the filtered list, or null.
    /// </summary>
    public Recipe? GetRecipeAt(int absoluteIndex)
    {
        return absoluteIndex >= 0 && absoluteIndex < _filteredRecipes.Count
            ? _filteredRecipes[absoluteIndex]
            : null;
    }

    /// <summary>
    /// Returns the currently selected recipe, or null.
    /// </summary>
    public Recipe? SelectedRecipe =>
        SelectedIndex.HasValue ? GetRecipeAt(SelectedIndex.Value) : null;
}
