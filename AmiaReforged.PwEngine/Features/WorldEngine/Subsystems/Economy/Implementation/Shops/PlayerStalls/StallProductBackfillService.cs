using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

/// <summary>
/// Service to backfill missing BaseItemType values for existing stall products.
/// Deserializes the stored item data to determine the item type, then updates the database.
/// </summary>
[ServiceBinding(typeof(StallProductBackfillService))]
public sealed class StallProductBackfillService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _repository;
    private bool _isRunning;

    public StallProductBackfillService(IPlayerShopRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <summary>
    /// Gets whether a backfill operation is currently in progress.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Runs the backfill process, updating all stall products that are missing a BaseItemType.
    /// </summary>
    /// <returns>A result containing counts of processed, updated, and failed products.</returns>
    public async Task<BackfillResult> RunBackfillAsync()
    {
        if (_isRunning)
        {
            return BackfillResult.AlreadyRunning();
        }

        _isRunning = true;
        int processed = 0;
        int updated = 0;
        int failed = 0;

        try
        {
            List<StallProduct> productsWithoutType = _repository.GetProductsWithoutItemType();

            if (productsWithoutType.Count == 0)
            {
                Log.Info("Stall product backfill: No products found without BaseItemType.");
                return new BackfillResult(true, 0, 0, 0, "No products require backfill.");
            }

            Log.Info("Stall product backfill: Found {Count} products without BaseItemType.", productsWithoutType.Count);

            foreach (StallProduct product in productsWithoutType)
            {
                processed++;

                try
                {
                    int? baseItemType = await ExtractBaseItemTypeAsync(product).ConfigureAwait(false);

                    if (baseItemType.HasValue)
                    {
                        bool success = _repository.UpdateProductBaseItemType(product.Id, baseItemType.Value);
                        if (success)
                        {
                            updated++;
                            Log.Debug("Backfill: Updated product {ProductId} ({Name}) with BaseItemType {Type}.",
                                product.Id, product.Name, baseItemType.Value);
                        }
                        else
                        {
                            failed++;
                            Log.Warn("Backfill: Failed to persist BaseItemType for product {ProductId}.", product.Id);
                        }
                    }
                    else
                    {
                        failed++;
                        Log.Warn("Backfill: Could not extract BaseItemType for product {ProductId} ({Name}).",
                            product.Id, product.Name);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    Log.Warn(ex, "Backfill: Exception processing product {ProductId} ({Name}).",
                        product.Id, product.Name);
                }

                // Yield to prevent blocking the main thread for too long
                if (processed % 10 == 0)
                {
                    await NwTask.Delay(TimeSpan.FromMilliseconds(50)).ConfigureAwait(false);
                }
            }

            string message = $"Backfill complete: {updated} updated, {failed} failed out of {processed} processed.";
            Log.Info("Stall product backfill: {Message}", message);

            return new BackfillResult(true, processed, updated, failed, message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Stall product backfill failed with an unexpected error.");
            return new BackfillResult(false, processed, updated, failed, $"Backfill failed: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Extracts the BaseItemType from a stall product by deserializing its ItemData.
    /// Creates a temporary item at the module's starting location, reads the type, then destroys it.
    /// </summary>
    private static async Task<int?> ExtractBaseItemTypeAsync(StallProduct product)
    {
        if (product.ItemData is null || product.ItemData.Length == 0)
        {
            return null;
        }

        await NwTask.SwitchToMainThread();

        Location? startingLocation = NwModule.Instance.StartingLocation;
        if (startingLocation is null)
        {
            Log.Warn("Backfill: Module starting location is not available.");
            return null;
        }

        NwItem? tempItem = null;

        try
        {
            // Deserialize the item using the centralized helper that handles both
            // binary GFF (preferred) and legacy JSON formats
            tempItem = PlayerStallEventManager.DeserializeItem(product.ItemData, startingLocation);

            if (tempItem is null || !tempItem.IsValid)
            {
                return null;
            }

            int baseItemType = (int)tempItem.BaseItem.ItemType;
            return baseItemType;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Backfill: Failed to deserialize item data for product {ProductId}.", product.Id);
            return null;
        }
        finally
        {
            if (tempItem is { IsValid: true })
            {
                tempItem.Destroy();
            }
        }
    }
}

/// <summary>
/// Result of a stall product backfill operation.
/// </summary>
public sealed record BackfillResult(
    bool Success,
    int Processed,
    int Updated,
    int Failed,
    string Message)
{
    public static BackfillResult AlreadyRunning() =>
        new(false, 0, 0, 0, "A backfill operation is already in progress.");
}
