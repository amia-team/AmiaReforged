using System.Text;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;

/// <summary>
/// One-time migration service that identifies warehouse items stored as legacy JSON bytes
/// and re-serializes them as binary GFF (the canonical format for bank storage).
///
/// Detection: JSON payloads always start with <c>{</c> (0x7B). Binary GFF starts with
/// a 4-char type header (e.g. <c>UTI </c>). A simple first-byte check is sufficient.
///
/// Conversion: Each legacy record is parsed via <see cref="Json.Parse"/> and materialised
/// at <see cref="NwModule.Instance.StartingLocation"/> so that <see cref="NwItem.Serialize"/>
/// can produce the binary GFF payload. The record is then updated in-place and the
/// temporary item is destroyed.
///
/// Runs automatically on module load. Idempotent — re-running is safe because converted
/// records no longer start with <c>{</c>.
/// </summary>
[ServiceBinding(typeof(LegacyStoredItemConversionService))]
public sealed class LegacyStoredItemConversionService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>Batch size for database queries to avoid loading everything at once.</summary>
    private const int BatchSize = 50;

    private readonly PwContextFactory _contextFactory;
    private bool _isRunning;

    public LegacyStoredItemConversionService(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    private async void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        try
        {
            ConversionResult result = await RunConversionAsync();

            if (result.Converted > 0 || result.Failed > 0)
            {
                Log.Info("Legacy stored-item conversion on module load: {Message}", result.Message);
            }
            else
            {
                Log.Debug("Legacy stored-item conversion: No legacy JSON records found.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Legacy stored-item conversion failed during module load.");
        }
    }

    /// <summary>Whether a conversion run is currently in progress.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Scans all <c>WarehouseItems</c> for records whose <c>ItemData</c> is stored as
    /// JSON and converts them to binary GFF in place.
    /// </summary>
    /// <remarks>
    /// Thread discipline: Every <c>await</c> that leaves the NWN main thread (EF Core calls)
    /// is followed by <c>await NwTask.SwitchToMainThread()</c> before any NWN engine access.
    /// Failing to do this will crash the server immediately.
    /// </remarks>
    public async Task<ConversionResult> RunConversionAsync()
    {
        if (_isRunning)
        {
            return new ConversionResult(false, 0, 0, 0, "A conversion is already in progress.");
        }

        _isRunning = true;
        int scanned = 0;
        int converted = 0;
        int failed = 0;

        try
        {
            while (true)
            {
                // ── DB read (off main thread after this await) ──
                List<StoredItem> batch;

                await using (PwEngineContext context = _contextFactory.CreateDbContext())
                {
                    // Fetch stored items whose payload starts with '{' (0x7B) — i.e. JSON.
                    // PostgreSQL get_byte returns the byte at a zero-based offset.
                    // 123 == 0x7B == '{'.
                    batch = await context.WarehouseItems
                        .FromSqlRaw(
                            """
                            SELECT * FROM "WarehouseItems"
                            WHERE octet_length("ItemData") > 0
                              AND get_byte("ItemData", 0) = 123
                            LIMIT {0}
                            """,
                            BatchSize)
                        .AsNoTracking()
                        .ToListAsync();
                }

                // ── Back to main thread before touching any NWN APIs ──
                await NwTask.SwitchToMainThread();

                if (batch.Count == 0)
                {
                    break;
                }

                foreach (StoredItem record in batch)
                {
                    scanned++;

                    try
                    {
                        // ConvertSingleItemAsync switches to main thread internally
                        // and all NWN work happens there. It returns pure byte[] data.
                        byte[]? gffBytes = await ConvertSingleItemAsync(record);

                        // After ConvertSingleItemAsync we are still on main thread,
                        // but the next DB calls will leave it.

                        if (gffBytes is null)
                        {
                            failed++;
                            Log.Warn(
                                "Legacy conversion: Could not convert StoredItem {ItemId} ('{Name}').",
                                record.Id, record.Name);
                            continue;
                        }

                        // ── DB write (off main thread after these awaits) ──
                        await using PwEngineContext writeContext = _contextFactory.CreateDbContext();
                        StoredItem? tracked = await writeContext.WarehouseItems
                            .FirstOrDefaultAsync(i => i.Id == record.Id);

                        if (tracked is null)
                        {
                            // Record was deleted between the read and now — skip.
                            failed++;
                            // Return to main thread before next loop iteration.
                            await NwTask.SwitchToMainThread();
                            continue;
                        }

                        tracked.ItemData = gffBytes;
                        await writeContext.SaveChangesAsync();

                        // ── Back to main thread before next iteration ──
                        await NwTask.SwitchToMainThread();

                        converted++;
                        Log.Info(
                            "Legacy conversion: Converted StoredItem {ItemId} ('{Name}') from JSON to binary GFF.",
                            record.Id, record.Name);
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Log.Error(ex,
                            "Legacy conversion: Unhandled error converting StoredItem {ItemId}.", record.Id);

                        // Ensure we're back on main thread even after an exception,
                        // so the next iteration's ConvertSingleItemAsync is safe.
                        await NwTask.SwitchToMainThread();
                    }
                }
            }

            string summary = $"Conversion complete. Scanned: {scanned}, Converted: {converted}, Failed: {failed}.";
            Log.Info("Legacy stored-item conversion: {Summary}", summary);
            return new ConversionResult(true, scanned, converted, failed, summary);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Legacy stored-item conversion failed unexpectedly.");
            return new ConversionResult(false, scanned, converted, failed,
                $"Conversion aborted: {ex.Message}");
        }
        finally
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Deserializes a single JSON-encoded item at the module start location, captures
    /// its binary GFF representation, then destroys the temporary item.
    /// </summary>
    private static async Task<byte[]?> ConvertSingleItemAsync(StoredItem record)
    {
        await NwTask.SwitchToMainThread();

        Location? startLocation = NwModule.Instance.StartingLocation;
        if (startLocation is null)
        {
            Log.Error("Legacy conversion: Module starting location is null — cannot convert items.");
            return null;
        }

        NwItem? tempItem = null;

        try
        {
            string jsonText = Encoding.UTF8.GetString(record.ItemData);
            Json json = Json.Parse(jsonText);
            tempItem = json.ToNwObject<NwItem>(startLocation);

            if (tempItem is null || !tempItem.IsValid)
            {
                Log.Warn("Legacy conversion: JSON produced an invalid item for StoredItem {ItemId}.", record.Id);
                return null;
            }

            // Capture the canonical binary GFF representation.
            byte[] gffBytes = tempItem.Serialize();

            if (gffBytes.Length == 0)
            {
                Log.Warn("Legacy conversion: item.Serialize() returned empty for StoredItem {ItemId}.", record.Id);
                return null;
            }

            return gffBytes;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Legacy conversion: Failed to convert StoredItem {ItemId} from JSON.", record.Id);
            return null;
        }
        finally
        {
            // Always clean up the temporary item from the start location.
            if (tempItem is { IsValid: true })
            {
                tempItem.Destroy();
            }
        }
    }
}

/// <summary>
/// Result of a legacy stored-item conversion run.
/// </summary>
public sealed record ConversionResult(
    bool Success,
    int Scanned,
    int Converted,
    int Failed,
    string Message);
