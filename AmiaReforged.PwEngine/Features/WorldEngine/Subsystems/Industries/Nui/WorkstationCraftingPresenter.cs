using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Nui;

/// <summary>
/// Presenter for the workstation crafting NUI window.
/// Handles recipe list display, search filtering, pagination, detail display,
/// and placeholder craft action. Auto-closes when the player moves away.
/// </summary>
public sealed class WorkstationCraftingPresenter : ScryPresenter<WorkstationCraftingView>, IAutoCloseOnMove
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly WorkstationTag _workstationTag;
    private readonly string _workstationName;

    private NuiWindowToken _token;
    private NuiWindow? _window;
    private WorkstationCraftingModel? _model;

    [Inject] private Lazy<IIndustrySubsystem>? IndustrySubsystem { get; init; }
    [Inject] private Lazy<RuntimeCharacterService>? CharacterService { get; init; }

    public WorkstationCraftingPresenter(
        WorkstationCraftingView view,
        NwPlayer player,
        WorkstationTag workstationTag,
        string workstationName)
    {
        View = view;
        _player = player;
        _workstationTag = workstationTag;
        _workstationName = workstationName;
    }

    public override WorkstationCraftingView View { get; }
    public override NuiWindowToken Token() => _token;

    // IAutoCloseOnMove defaults: 1s poll, 0.1m threshold

    public override void InitBefore()
    {
        _model = new WorkstationCraftingModel(_workstationTag, _workstationName);
        _window = new NuiWindow(View.RootLayout(), _workstationName)
        {
            Geometry = new NuiRect(40f, 40f, WorkstationCraftingView.WindowW, WorkstationCraftingView.WindowH),
            Resizable = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage("Workstation window not configured.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Unable to open workstation window.", ColorConstants.Orange);
            return;
        }

        // Resolve character ID
        CharacterId? characterId = ResolveCharacterId();
        if (characterId == null)
        {
            _player.SendServerMessage("No character key found.", ColorConstants.Orange);
            SetDetailContent("Error", "No character key found. Ensure you have a valid character.");
            return;
        }

        // Load recipes
        List<Recipe> recipes;
        try
        {
            recipes = IndustrySubsystem!.Value
                .GetWorkstationRecipesAsync(characterId.Value, _workstationTag)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load workstation recipes for {Tag}", _workstationTag.Value);
            recipes = [];
        }

        _model!.Load(recipes);

        // Set up search watch
        _token.SetBindWatch(View.SearchText, true);
        _token.SetBindValue(View.SearchText, string.Empty);

        // Initial UI state
        RefreshEntryList();
        SetDetailContent("Select a Recipe", "Choose a recipe from the list to view its details.");
        _token.SetBindValue(View.ShowCraftButton, false);
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        // Handle search watch
        if (eventData.EventType == NuiEventType.Watch && eventData.ElementId == View.SearchText.Key)
        {
            string searchTerm = (_token.GetBindValue(View.SearchText) ?? string.Empty).Trim();
            _model!.ApplySearch(searchTerm);
            RefreshEntryList();
            SetDetailContent("Select a Recipe", "Choose a recipe from the list to view its details.");
            _token.SetBindValue(View.ShowCraftButton, false);
            return;
        }

        if (eventData.EventType != NuiEventType.Click) return;
        HandleClick(eventData.ElementId);
    }

    public override void Close()
    {
        try { _token.Close(); } catch { /* token may already be invalid */ }
    }

    // --- Click Handling ---

    private void HandleClick(string elementId)
    {
        switch (elementId)
        {
            case "btn_close":
                RaiseCloseEvent();
                Close();
                break;

            case "btn_clear_search":
                _token.SetBindValue(View.SearchText, string.Empty);
                _model!.ApplySearch(string.Empty);
                RefreshEntryList();
                SetDetailContent("Select a Recipe", "Choose a recipe from the list to view its details.");
                _token.SetBindValue(View.ShowCraftButton, false);
                break;

            case "btn_prev_page":
                if (_model!.CurrentPage > 0)
                {
                    _model.CurrentPage--;
                    RefreshEntryList();
                }
                break;

            case "btn_next_page":
                if (_model!.CurrentPage < _model.TotalPages - 1)
                {
                    _model.CurrentPage++;
                    RefreshEntryList();
                }
                break;

            case "btn_craft":
                HandleCraft();
                break;

            default:
                if (elementId.StartsWith("btn_recipe_") && int.TryParse(elementId["btn_recipe_".Length..], out int rowIndex))
                    SelectEntry(rowIndex);
                break;
        }
    }

    // --- Entry List ---

    private void RefreshEntryList()
    {
        IReadOnlyList<Recipe> pageRecipes = _model!.CurrentPageRecipes;

        _token.SetBindValue(View.PageInfo, $"{_model.CurrentPage + 1} / {_model.TotalPages}");
        _token.SetBindValue(View.ShowPrevPage, _model.CurrentPage > 0);
        _token.SetBindValue(View.ShowNextPage, _model.CurrentPage < _model.TotalPages - 1 && _model.FilteredRecipes.Count > 0);

        for (int i = 0; i < WorkstationCraftingView.EntriesPerPage; i++)
        {
            if (i < pageRecipes.Count)
            {
                Recipe recipe = pageRecipes[i];
                _token.SetBindValue(View.EntryNames[i], recipe.Name);
                _token.SetBindValue(View.EntrySubtitles[i], FormatSubtitle(recipe));
                _token.SetBindValue(View.EntryRowVisible[i], true);
            }
            else
            {
                _token.SetBindValue(View.EntryRowVisible[i], false);
            }
        }
    }

    private static string FormatSubtitle(Recipe recipe)
    {
        int ingredientCount = recipe.Ingredients.Count;
        string knowledge = recipe.RequiredKnowledge.Count > 0
            ? string.Join(", ", recipe.RequiredKnowledge)
            : "none";
        return $"Knowledge: {knowledge} | {ingredientCount} ingredient{(ingredientCount != 1 ? "s" : "")}";
    }

    // --- Selection & Detail ---

    private void SelectEntry(int rowIndex)
    {
        int absoluteIndex = (_model!.CurrentPage * WorkstationCraftingView.EntriesPerPage) + rowIndex;
        Recipe? recipe = _model.GetRecipeAt(absoluteIndex);
        if (recipe == null) return;

        _model.SelectedIndex = absoluteIndex;
        string body = FormatRecipeDetail(recipe);
        SetDetailContent(recipe.Name, body);
        _token.SetBindValue(View.ShowCraftButton, true);
    }

    private static string FormatRecipeDetail(Recipe recipe)
    {
        List<string> sections = [];

        // Description
        if (!string.IsNullOrWhiteSpace(recipe.Description))
            sections.Add(recipe.Description);

        // Industry & Knowledge
        sections.Add($"Industry: {recipe.IndustryTag.Value}");
        if (recipe.RequiredKnowledge.Count > 0)
            sections.Add($"Required Knowledge: {string.Join(", ", recipe.RequiredKnowledge)}");

        // Ingredients
        if (recipe.Ingredients.Count > 0)
        {
            sections.Add("\n--- Ingredients ---");
            foreach (Ingredient ingredient in recipe.Ingredients)
            {
                string consumed = ingredient.IsConsumed ? "" : " (not consumed)";
                string quality = ingredient.MinQuality.HasValue ? $" [min quality: {ingredient.MinQuality}]" : "";
                sections.Add($"  {ingredient.Quantity.Value}x {ingredient.ItemTag}{quality}{consumed}");
            }
        }

        // Products
        if (recipe.Products.Count > 0)
        {
            sections.Add("\n--- Products ---");
            foreach (Product product in recipe.Products)
            {
                string quality = product.Quality.HasValue ? $" [quality: {product.Quality}]" : "";
                string chance = product.SuccessChance.HasValue ? $" ({product.SuccessChance:P0} chance)" : "";
                sections.Add($"  {product.Quantity.Value}x {product.ItemTag}{quality}{chance}");
            }
        }

        // Crafting time
        if (recipe.CraftingTimeSeconds.HasValue)
            sections.Add($"\nCrafting Time: {recipe.CraftingTimeSeconds}s");

        // Knowledge points
        if (recipe.KnowledgePointsAwarded > 0)
            sections.Add($"Knowledge Points: +{recipe.KnowledgePointsAwarded}");

        return string.Join("\n", sections);
    }

    // --- Craft Action ---

    private void HandleCraft()
    {
        Recipe? recipe = _model?.SelectedRecipe;
        if (recipe == null)
        {
            _player.SendServerMessage("No recipe selected.", ColorConstants.Orange);
            return;
        }

        _player.SendServerMessage(
            $"Crafting '{recipe.Name}' is not yet implemented. The process-based crafting system is coming soon!",
            ColorConstants.Orange);
    }

    // --- Helpers ---

    private void SetDetailContent(string title, string body)
    {
        _token.SetBindValue(View.DetailTitle, title);
        _token.SetBindValue(View.DetailBody, body);
    }

    private CharacterId? ResolveCharacterId()
    {
        // First try RuntimeCharacterService (preferred — already cached)
        if (CharacterService?.Value != null &&
            CharacterService.Value.TryGetPlayerKey(_player, out Guid cachedKey) &&
            cachedKey != Guid.Empty)
        {
            return CharacterId.From(cachedKey);
        }

        // Fallback: parse from ds_pckey item
        try
        {
            NwItem? pcKey = _player.LoginCreature?.Inventory.Items
                .FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey == null) return null;

            string dbToken = pcKey.Name.Split("_")[1];
            if (!Guid.TryParse(dbToken, out Guid guid)) return null;
            return CharacterId.From(guid);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve CharacterId for workstation crafting");
            return null;
        }
    }
}
