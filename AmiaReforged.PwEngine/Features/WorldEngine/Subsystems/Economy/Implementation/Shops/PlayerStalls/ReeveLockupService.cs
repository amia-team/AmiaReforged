using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

public interface IReeveLockupRecipient
{
    Task<bool> ReceiveItemAsync(byte[] rawItemData, PersonaId persona, CancellationToken cancellationToken, int quantity = 1);
}

public sealed record ReeveLockupItemSummary(long ItemId, string DisplayName, string? ResRef);

/// <summary>
/// Persists lapsed stall inventory into long-term storage so the market reeve can
/// restore it for players after restarts or other interruptions.
/// </summary>
[ServiceBinding(typeof(ReeveLockupService))]
public sealed class ReeveLockupService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IDbContextFactory<PwEngineContext> _contextFactory;

    public ReeveLockupService(IDbContextFactory<PwEngineContext> contextFactory)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
    }

    /// <summary>
    /// Records the provided stall products in the reeve's lockup storage so they can be reclaimed later.
    /// </summary>
    /// <returns>The number of individual item copies written to storage.</returns>
    public async Task<int> StoreSuspendedInventoryAsync(
        PlayerStall stall,
        IReadOnlyCollection<StallProduct> products,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stall);
        ArgumentNullException.ThrowIfNull(products);

        if (products.Count == 0)
        {
            return 0;
        }

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        Database.Entities.Storage? storage = null;
        int storedCount = 0;

        foreach (StallProduct product in products)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryResolveOwnerGuid(stall, product, out Guid ownerGuid))
            {
                continue;
            }

            // Lazy-create storage only when we have valid items to store
            storage ??= await EnsureStorageAsync(context, stall.AreaResRef, cancellationToken).ConfigureAwait(false);

            int quantity = Math.Max(1, product.Quantity);

            for (int index = 0; index < quantity; index++)
            {
                StoredItem entry = new()
                {
                    ItemData = product.ItemData.ToArray(),
                    Owner = ownerGuid,
                    WarehouseId = storage.Id,
                    Name = product.Name,
                    Description = product.Description
                };

                await context.WarehouseItems.AddAsync(entry, cancellationToken).ConfigureAwait(false);
                storedCount++;
            }
        }

        if (storedCount == 0)
        {
            return 0;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Log.Info(
            "Stored {Count} stall items in reeve lockup for stall {StallId} (area '{AreaResRef}').",
            storedCount,
            stall.Id,
            stall.AreaResRef ?? "<unknown>");

        return storedCount;
    }

    /// <summary>
    /// Restores any stored inventory for the provided persona back into the given creature's possession.
    /// </summary>
    /// <returns>The number of individual item copies restored.</returns>
    public async Task<int> ReleaseInventoryToPlayerAsync(
        PersonaId persona,
        string? areaResRef,
        IReeveLockupRecipient recipient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        if (!TryResolveStorageContext(persona, areaResRef, out Guid ownerGuid, out Guid engineId))
        {
            return 0;
        }

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        List<StoredItem> items = await BuildStoredItemsQuery(context, ownerGuid, engineId, trackChanges: true)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (items.Count == 0)
        {
            return 0;
        }

        List<StoredItem> delivered = new(items.Count);
        int restoredCount = 0;

        foreach (StoredItem stored in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            bool restored = await recipient
                .ReceiveItemAsync(stored.ItemData, persona, cancellationToken)
                .ConfigureAwait(false);

            if (!restored)
            {
                continue;
            }

            delivered.Add(stored);
            restoredCount++;
        }

        if (delivered.Count == 0)
        {
            return 0;
        }

        context.WarehouseItems.RemoveRange(delivered);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Log.Info(
            "Released {Count} stall items from reeve lockup in area '{AreaResRef}' to persona {Persona}.",
            restoredCount,
            areaResRef ?? "<unknown>",
            persona);

        return restoredCount;
    }

    public async Task<IReadOnlyList<ReeveLockupItemSummary>> ListStoredInventoryAsync(
        PersonaId persona,
        string? areaResRef,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveStorageContext(persona, areaResRef, out Guid ownerGuid, out Guid engineId))
        {
            return Array.Empty<ReeveLockupItemSummary>();
        }

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        List<StoredItem> items = await BuildStoredItemsQuery(context, ownerGuid, engineId, trackChanges: false)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (items.Count == 0)
        {
            return Array.Empty<ReeveLockupItemSummary>();
        }

        List<ReeveLockupItemSummary> summaries = new(items.Count);

        foreach (StoredItem item in items)
        {
            // Extract metadata from JSON payload for display
            (string jsonName, string? resRef) = ExtractItemMetadata(item.ItemData);

            // Use JSON name if available, otherwise fall back to stored Name, then "Stored Item"
            string displayName = !string.IsNullOrWhiteSpace(jsonName) && jsonName != "Stored Item"
                ? jsonName
                : (!string.IsNullOrWhiteSpace(item.Name) ? item.Name : "Stored Item");

            summaries.Add(new ReeveLockupItemSummary(item.Id, displayName, resRef));
        }

        return summaries;
    }

    public async Task<int> CountStoredInventoryAsync(
        PersonaId persona,
        string? areaResRef,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveStorageContext(persona, areaResRef, out Guid ownerGuid, out Guid engineId))
        {
            return 0;
        }

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        return await BuildStoredItemsQuery(context, ownerGuid, engineId, trackChanges: false)
            .CountAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> ReleaseStoredItemAsync(
        long storedItemId,
        PersonaId persona,
        string? areaResRef,
        IReeveLockupRecipient recipient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        if (!TryResolveStorageContext(persona, areaResRef, out Guid ownerGuid, out Guid engineId))
        {
            return false;
        }

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        StoredItem? stored = await context.WarehouseItems
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(
                i => i.Id == storedItemId &&
                     i.Owner == ownerGuid &&
                     i.Warehouse != null &&
                     i.Warehouse.EngineId == engineId,
                cancellationToken)
            .ConfigureAwait(false);

        if (stored is null)
        {
            return false;
        }

        bool restored = await recipient
            .ReceiveItemAsync(stored.ItemData, persona, cancellationToken)
            .ConfigureAwait(false);

        if (!restored)
        {
            return false;
        }

        context.WarehouseItems.Remove(stored);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Log.Info(
            "Released stored stall item {StoredItemId} from reeve lockup in area '{AreaResRef}' to persona {Persona}.",
            storedItemId,
            areaResRef ?? "<unknown>",
            persona);

        return true;
    }

    private static Guid ComputeStorageEngineId(string? areaResRef)
    {
        string normalized = string.IsNullOrWhiteSpace(areaResRef)
            ? "market-reeve:unknown"
            : $"market-reeve:{areaResRef.Trim().ToLowerInvariant()}";

        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));

        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);

        return new Guid(guidBytes);
    }

    private bool TryResolveStorageContext(
        PersonaId persona,
        string? areaResRef,
        out Guid ownerGuid,
        out Guid engineId)
    {
        ownerGuid = Guid.Empty;
        engineId = Guid.Empty;

        try
        {
            ownerGuid = PersonaId.ToGuid(persona);
        }
        catch (Exception ex)
        {
            Log.Warn(
                ex,
                "Unable to convert persona {Persona} into a storage owner when accessing reeve inventory.",
                persona);
            return false;
        }

        engineId = ComputeStorageEngineId(areaResRef);
        return true;
    }

    private static IQueryable<StoredItem> BuildStoredItemsQuery(
        PwEngineContext context,
        Guid ownerGuid,
        Guid engineId,
        bool trackChanges)
    {
        IQueryable<StoredItem> query = context.WarehouseItems
            .Include(i => i.Warehouse)
            .Where(i => i.Owner == ownerGuid && i.Warehouse != null && i.Warehouse.EngineId == engineId)
            .OrderBy(i => i.Id);

        return trackChanges ? query : query.AsNoTracking();
    }

    private static async Task<Database.Entities.Storage> EnsureStorageAsync(
        PwEngineContext context,
        string? areaResRef,
        CancellationToken cancellationToken)
    {
        Guid engineId = ComputeStorageEngineId(areaResRef);

        Database.Entities.Storage? existing = await context.Warehouses
            .FirstOrDefaultAsync(w => w.EngineId == engineId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        Database.Entities.Storage storage = new()
        {
            EngineId = engineId,
            Capacity = -1,
            StorageType = "PlayerInventory"  // Default to PlayerInventory as per database schema
        };

        await context.Warehouses.AddAsync(storage, cancellationToken).ConfigureAwait(false);

        // Save immediately to generate the ID that will be used as foreign key by WarehouseItems
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return storage;
    }

    private bool TryResolveOwnerGuid(PlayerStall stall, StallProduct product, out Guid ownerGuid)
    {
        ownerGuid = Guid.Empty;

        string? personaId = !string.IsNullOrWhiteSpace(product.ConsignedByPersonaId)
            ? product.ConsignedByPersonaId
            : stall.OwnerPersonaId;

        if (string.IsNullOrWhiteSpace(personaId))
        {
            Log.Warn(
                "Skipping reeve storage for product {ProductId} from stall {StallId}: no consignor persona.",
                product.Id,
                stall.Id);
            return false;
        }

        try
        {
            PersonaId persona = PersonaId.Parse(personaId);
            ownerGuid = PersonaId.ToGuid(persona);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn(
                ex,
                "Failed to map persona '{PersonaId}' to a storage owner for product {ProductId} in stall {StallId}.",
                personaId,
                product.Id,
                stall.Id);
            return false;
        }
    }
    private static (string Label, string? ResRef) ExtractItemMetadata(byte[] rawItemData)
    {
        try
        {
            string jsonText = Encoding.UTF8.GetString(rawItemData);
            using JsonDocument document = JsonDocument.Parse(jsonText);
            JsonElement root = document.RootElement;

            string? label = TryFindStringValue(root, MetadataNameCandidates) ??
                            TryFindStringValue(root, MetadataTagCandidates);

            string? resRef = TryFindStringValue(root, MetadataResRefCandidates);

            if (string.IsNullOrWhiteSpace(label))
            {
                label = "Stored Item";
            }

            return (label!, string.IsNullOrWhiteSpace(resRef) ? null : resRef);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to derive metadata for reeve lockup item payload.");
            return ("Stored Item", null);
        }
    }

    private static string? TryFindStringValue(JsonElement element, IReadOnlyList<string> candidateNames)
    {
        foreach (string candidate in candidateNames)
        {
            if (TryFindStringValue(element, candidate, out string? value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool TryFindStringValue(JsonElement element, string targetName, out string? value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    if (property.Value.ValueKind == JsonValueKind.String)
                    {
                        value = property.Value.GetString();
                        return true;
                    }

                    if (TryExtractNestedString(property.Value, out value))
                    {
                        return true;
                    }
                }

                if (TryFindStringValue(property.Value, targetName, out value))
                {
                    return true;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                if (TryFindStringValue(child, targetName, out value))
                {
                    return true;
                }
            }
        }

        value = null;
        return false;
    }

    private static bool TryExtractNestedString(JsonElement element, out string? value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("String", out JsonElement stringElement) && stringElement.ValueKind == JsonValueKind.String)
            {
                value = stringElement.GetString();
                return true;
            }

            if (element.TryGetProperty("Value", out JsonElement valueElement) && valueElement.ValueKind == JsonValueKind.String)
            {
                value = valueElement.GetString();
                return true;
            }

            if (element.TryGetProperty("Text", out JsonElement textElement) && textElement.ValueKind == JsonValueKind.String)
            {
                value = textElement.GetString();
                return true;
            }
        }

        value = null;
        return false;
    }

    private static readonly IReadOnlyList<string> MetadataNameCandidates = new[] { "LocalizedName", "DisplayName", "Name", "Label" };
    private static readonly IReadOnlyList<string> MetadataTagCandidates = new[] { "Tag" };
    private static readonly IReadOnlyList<string> MetadataResRefCandidates = new[] { "ResRef", "TemplateResRef" };

}

internal sealed class NwReeveLockupRecipient : IReeveLockupRecipient
{
    private static readonly Logger Log = LogManager.GetLogger(nameof(NwReeveLockupRecipient));

    private readonly NwCreature _creature;

    public NwReeveLockupRecipient(NwCreature creature)
    {
        _creature = creature ?? throw new ArgumentNullException(nameof(creature));
    }

    public async Task<bool> ReceiveItemAsync(byte[] rawItemData, PersonaId persona, CancellationToken cancellationToken, int quantity = 1)
    {
        ArgumentNullException.ThrowIfNull(rawItemData);

        await NwTask.SwitchToMainThread();

        if (_creature is not { IsValid: true })
        {
            return false;
        }

        Location? location = _creature.Location;
        if (location is null)
        {
            return false;
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Deserialize the item using the centralized helper that handles both
            // binary GFF (preferred) and legacy JSON formats
            NwItem? item = PlayerStallEventManager.DeserializeItem(rawItemData, location);
            if (item is null)
            {
                return false;
            }

            // Set the stack size before transferring to inventory.
            // Each StoredItem entry represents a single unit, so default quantity is 1.
            item.StackSize = Math.Max(1, quantity);
            _creature.AcquireItem(item);

            NWScript.SetLocalString(item, PlayerStallItemLocals.ConsignorPersonaId, persona.ToString());
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to restore stored stall item from reeve lockup.");
            return false;
        }
    }
}
