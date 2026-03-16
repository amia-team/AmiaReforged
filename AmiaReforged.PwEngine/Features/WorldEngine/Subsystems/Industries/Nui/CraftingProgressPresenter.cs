using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
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

    private NuiWindowToken _token;
    private NuiWindow? _window;

    private int _totalSeconds;
    private int _elapsedSeconds;
    private bool _cancelled;

    [Inject] private Lazy<IIndustrySubsystem>? IndustrySubsystem { get; init; }

    public CraftingProgressPresenter(
        CraftingProgressView view,
        NwPlayer player,
        Recipe recipe,
        AggregatedCraftingModifiers modifiers,
        CharacterId characterId)
    {
        View = view;
        _player = player;
        _recipe = recipe;
        _modifiers = modifiers;
        _characterId = characterId;
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

                // Guard: player disconnected or window was closed
                if (_cancelled || !_player.IsValid)
                {
                    Log.Info("Crafting cancelled or player disconnected for recipe '{Recipe}'", _recipe.Name);
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

                // Show success message
                string productSummary = FormatProductSummary();
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
    /// Collects quality values from inventory items matching each ingredient slot.
    /// Returns null for ingredients that don't have a quality property.
    /// </summary>
    private List<int?> CollectInputQualities()
    {
        List<int?> qualities = [];
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return qualities;

        foreach (Ingredient ingredient in _recipe.Ingredients)
        {
            NwItem? matchingItem = creature.Inventory.Items
                .FirstOrDefault(item => item.Tag == ingredient.ItemTag);

            if (matchingItem != null)
            {
                // Try to read quality from the item's local variable or item property
                int qualityValue = matchingItem.GetObjectVariable<LocalVariableInt>("we_quality").Value;
                qualities.Add(qualityValue > 0 ? qualityValue : null);
            }
            else
            {
                qualities.Add(null);
            }
        }

        return qualities;
    }

    /// <summary>
    /// Consumes ingredients from the player's inventory based on the recipe requirements.
    /// Only consumes ingredients marked with <see cref="Ingredient.IsConsumed"/> = true.
    /// </summary>
    private void ConsumeIngredients()
    {
        NwCreature? creature = _player.LoginCreature;
        if (creature == null) return;

        foreach (Ingredient ingredient in _recipe.Ingredients)
        {
            if (!ingredient.IsConsumed) continue;

            int remaining = ingredient.Quantity.Value;

            // Find and consume matching items from inventory
            foreach (NwItem item in creature.Inventory.Items.ToList())
            {
                if (remaining <= 0) break;
                if (item.Tag != ingredient.ItemTag) continue;

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
    private string FormatProductSummary()
    {
        if (_recipe.Products.Count == 0) return "";

        List<string> parts = _recipe.Products
            .Select(p => $"{p.Quantity.Value}x {p.ItemTag}")
            .ToList();

        return string.Join(", ", parts);
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
