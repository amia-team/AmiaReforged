using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Nui;

/// <summary>
/// Presenter for the crafting progress bar window. Runs a timed loop that increments the
/// progress bar each second, then executes the craft command on completion. The window
/// cannot be closed by the player — only the "Done" button (post-completion) dismisses it.
/// <para>
/// Does NOT implement <see cref="IAutoCloseOnMove"/> — crafting should persist even if the
/// player moves away from the workstation.
/// </para>
/// </summary>
public sealed class CraftingProgressPresenter : ScryPresenter<CraftingProgressView>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly NwPlayer _player;
    private readonly Recipe _recipe;
    private readonly AggregatedCraftingModifiers _modifiers;
    private readonly CharacterId _characterId;
    private readonly List<int> _selectedQualities;

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private int _totalSeconds;
    private int _elapsedSeconds;
    private bool _cancelled;

    [Inject] private Lazy<IIndustrySubsystem>? IndustrySubsystem { get; init; }
    [Inject] private Lazy<IItemDefinitionRepository>? ItemDefinitionRepository { get; init; }

    public CraftingProgressPresenter(
        CraftingProgressView view,
        NwPlayer player,
        Recipe recipe,
        AggregatedCraftingModifiers modifiers,
        CharacterId characterId,
        List<int> selectedQualities)
    {
        View = view;
        _player = player;
        _recipe = recipe;
        _modifiers = modifiers;
        _characterId = characterId;
        _selectedQualities = selectedQualities;
    }

    public override CraftingProgressView View { get; }
    public override NuiWindowToken Token() => _token;

    public override void InitBefore()
    {
        // Compute effective crafting time in seconds: max(1, baseRounds - timeReduction) * 6
        int baseRounds = _recipe.CraftingTimeRounds ?? 1;
        int effectiveRounds = Math.Max(1, baseRounds - _modifiers.TimeReductionRounds);
        _totalSeconds = effectiveRounds * 6;
        _elapsedSeconds = 0;
        _cancelled = false;

        _window = new NuiWindow(View.RootLayout(), $"Crafting: {_recipe.Name}")
        {
            Geometry = new NuiRect(-1f, -1f, CraftingProgressView.WindowW, CraftingProgressView.WindowH),
            Closable = false,
            Resizable = false,
            Collapsed = false,
            Border = true
        };
    }

    public override void Create()
    {
        if (_window == null)
        {
            _player.SendServerMessage("Crafting progress window not configured.", ColorConstants.Orange);
            return;
        }

        if (!_player.TryCreateNuiWindow(_window, out _token))
        {
            _player.SendServerMessage("Unable to open crafting progress window.", ColorConstants.Orange);
            return;
        }

        // Set initial bind values
        _token.SetBindValue(View.StatusText, $"Crafting {_recipe.Name}...");
        _token.SetBindValue(View.ProgressValue, 0f);
        _token.SetBindValue(View.TimeRemainingText, FormatTimeRemaining(_totalSeconds));
        _token.SetBindValue(View.ShowDoneButton, false);

        // Start the timed crafting loop
        StartCraftingLoop();
    }

    public override void ProcessEvent(ModuleEvents.OnNuiEvent eventData)
    {
        if (eventData.EventType != NuiEventType.Click) return;

        if (eventData.ElementId == "btn_done")
        {
            RaiseCloseEvent();
            Close();
        }
    }

    public override void Close()
    {
        _cancelled = true;
        try { _token.Close(); } catch { /* token may already be invalid */ }
    }

    // --- Crafting Timer Loop ---

    private void StartCraftingLoop()
    {
        _ = NwTask.Run(async () =>
        {
            for (int second = 0; second < _totalSeconds; second++)
            {
                await NwTask.Delay(TimeSpan.FromSeconds(1));
                await NwTask.SwitchToMainThread();

                // Guard: player disconnected, window was closed, or player died
                if (_cancelled || !_player.IsValid)
                {
                    Log.Info("Crafting cancelled or player disconnected for recipe '{Recipe}'", _recipe.Name);
                    return;
                }

                if (_player.LoginCreature is { IsDead: true })
                {
                    Log.Info("Player died during crafting of recipe '{Recipe}'", _recipe.Name);
                    _player.SendServerMessage("Crafting failed — you died.", ColorConstants.Red);
                    _cancelled = true;
                    Close();
                    return;
                }

                _elapsedSeconds = second + 1;
                float progress = _elapsedSeconds / (float)_totalSeconds;
                int remaining = _totalSeconds - _elapsedSeconds;

                _token.SetBindValue(View.ProgressValue, progress);
                _token.SetBindValue(View.TimeRemainingText, FormatTimeRemaining(remaining));
            }

            // Timer complete — execute the craft
            if (!_cancelled && _player.IsValid)
            {
                await HandleCraftingComplete();
            }
        });
    }

    private async Task HandleCraftingComplete()
    {
        // Ensure we're on the main thread for NWN API calls
        await NwTask.SwitchToMainThread();

        if (_cancelled || !_player.IsValid) return;

        _token.SetBindValue(View.StatusText, "Finishing...");
        _token.SetBindValue(View.ProgressValue, 1.0f);
        _token.SetBindValue(View.TimeRemainingText, "");

        // Build the craft command
        // Collect input qualities from the player's inventory items matching ingredients
        List<int?> inputQualities = CollectInputQualities();

        CraftItemCommand command = new()
        {
            CharacterId = _characterId,
            IndustryTag = _recipe.IndustryTag,
            RecipeId = _recipe.RecipeId,
            InputQualities = inputQualities
        };

        try
        {
            CommandResult result = await IndustrySubsystem!.Value.CraftItemAsync(command);

            if (result.Success)
            {
                // Consume ingredients from inventory
                ConsumeIngredients();

                // Grant products to the player
                List<Product> products = result.Data?.GetValueOrDefault("products") as List<Product>
                    ?? _recipe.Products;
                GrantProducts(products);

                // Show success message
                string productSummary = FormatProductSummary(products);
                _token.SetBindValue(View.StatusText, "Crafting Complete!");
                _token.SetBindValue(View.TimeRemainingText, productSummary);

                _player.SendServerMessage(
                    $"Successfully crafted: {_recipe.Name}",
                    ColorConstants.Lime);
            }
            else
            {
                _token.SetBindValue(View.StatusText, "Crafting Failed");
                _token.SetBindValue(View.TimeRemainingText, result.ErrorMessage ?? "Unknown error");

                _player.SendServerMessage(
                    $"Crafting failed: {result.ErrorMessage}",
                    ColorConstants.Red);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing CraftItemAsync for recipe '{Recipe}'", _recipe.Name);
            _token.SetBindValue(View.StatusText, "Crafting Error");
            _token.SetBindValue(View.TimeRemainingText, "An unexpected error occurred.");

            _player.SendServerMessage(
                "An error occurred during crafting. Please try again.",
                ColorConstants.Red);
        }

        // Show the Done button
        _token.SetBindValue(View.ShowDoneButton, true);
    }

    // --- Ingredient Helpers ---

    /// <summary>
    /// Returns the selected quality tier per ingredient, as chosen by the player.
    /// Falls back to 0 (Unknown) if no selection exists for a given index.
    /// </summary>
    private List<int?> CollectInputQualities()
    {
        List<int?> qualities = [];

        for (int i = 0; i < _recipe.Ingredients.Count; i++)
        {
            int quality = i < _selectedQualities.Count ? _selectedQualities[i] : CraftingQuality.Unknown;
            qualities.Add(quality > 0 ? quality : null);
        }

        return qualities;
    }

    /// <summary>
    /// Consumes ingredients from the player's inventory based on the recipe requirements.
    /// Only consumes ingredients marked with <see cref="Ingredient.IsConsumed"/> = true.
    /// Filters by the player-selected quality tier for each ingredient.
    /// </summary>
    private void ConsumeIngredients()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        for (int i = 0; i < _recipe.Ingredients.Count; i++)
        {
            Ingredient ingredient = _recipe.Ingredients[i];
            if (!ingredient.IsConsumed) continue;

            int selectedQuality = i < _selectedQualities.Count ? _selectedQualities[i] : CraftingQuality.Unknown;
            int remaining = ingredient.Quantity.Value;

            // Find and consume matching items with the selected quality tier
            foreach (NwItem item in creature.Inventory.Items.ToList())
            {
                if (remaining <= 0) break;
                if (item.Tag != ingredient.ItemTag) continue;
                if (item.GetObjectVariable<LocalVariableInt>("item_quality").Value != selectedQuality) continue;

                if (item.StackSize <= remaining)
                {
                    remaining -= item.StackSize;
                    item.Destroy();
                }
                else
                {
                    item.StackSize -= remaining;
                    remaining = 0;
                }
            }
        }
    }

    /// <summary>
    /// Formats a summary of the products that will be created.
    /// </summary>
    private string FormatProductSummary(List<Product> products)
    {
        if (products.Count == 0) return "";

        List<string> parts = products
            .Select(p => $"{p.Quantity.Value}x {p.ItemTag}")
            .ToList();

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Creates product items in the player's inventory based on the crafting result.
    /// Uses the same item-creation pattern as <see cref="ResourceHarvestedEventHandler"/>.
    /// </summary>
    private void GrantProducts(List<Product> products)
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        IItemDefinitionRepository? itemRepo = ItemDefinitionRepository?.Value;
        if (itemRepo == null)
        {
            Log.Error("IItemDefinitionRepository not available — cannot grant crafted products");
            return;
        }

        foreach (Product product in products)
        {
            // Roll success chance if applicable
            if (product.SuccessChance.HasValue)
            {
                float roll = Random.Shared.NextSingle();
                if (roll > product.SuccessChance.Value) continue;
            }

            ItemBlueprint? blueprint = itemRepo.GetByTag(product.ItemTag);
            if (blueprint == null)
            {
                Log.Warn("Item blueprint '{ItemTag}' not found — skipping product", product.ItemTag);
                continue;
            }

            IPQuality quality = (IPQuality)(product.Quality ?? (int)IPQuality.Average);
            ItemDto itemDto = new(blueprint, quality, quality);

            RuntimeInventoryPort inventoryPort = new(creature);
            for (int i = 0; i < product.Quantity.Value; i++)
            {
                inventoryPort.AddItem(itemDto);
            }

            Log.Debug("Granted {Qty}x {Tag} (Q: {Quality}) to player {Player}",
                product.Quantity.Value, product.ItemTag, quality, _player.PlayerName);
        }
    }

    /// <summary>
    /// Formats seconds remaining into a human-readable string.
    /// </summary>
    private static string FormatTimeRemaining(int seconds)
    {
        if (seconds <= 0) return "Complete";

        return seconds >= 60
            ? $"{seconds / 60}m {seconds % 60}s remaining"
            : $"{seconds}s remaining";
    }
}
