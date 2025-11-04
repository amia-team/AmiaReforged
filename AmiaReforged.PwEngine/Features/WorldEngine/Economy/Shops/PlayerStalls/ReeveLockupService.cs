using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Persists lapsed stall inventory into long-term storage so the market reeve can
/// restore it for players after restarts or other interruptions.
/// </summary>
[ServiceBinding(typeof(ReeveLockupService))]
internal sealed class ReeveLockupService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly PwContextFactory _contextFactory;

    public ReeveLockupService(PwContextFactory contextFactory)
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

        Storage storage = await EnsureStorageAsync(context, stall.AreaResRef, cancellationToken).ConfigureAwait(false);

        int storedCount = 0;

        foreach (StallProduct product in products)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!TryResolveOwnerGuid(stall, product, out Guid ownerGuid))
            {
                continue;
            }

            int quantity = Math.Max(1, product.Quantity);

            for (int index = 0; index < quantity; index++)
            {
                StoredItem entry = new()
                {
                    ItemData = product.ItemData.ToArray(),
                    Owner = ownerGuid,
                    Warehouse = storage
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
        NwCreature recipient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipient);

        Guid ownerGuid;
        try
        {
            ownerGuid = PersonaId.ToGuid(persona);
        }
        catch (Exception ex)
        {
            Log.Warn(
                ex,
                "Unable to convert persona {Persona} into a storage owner when releasing reeve inventory.",
                persona);
            return 0;
        }

        Guid engineId = ComputeStorageEngineId(areaResRef);

        await using PwEngineContext context = _contextFactory.CreateDbContext();

        List<StoredItem> items = await context.WarehouseItems
            .Include(i => i.Warehouse)
            .Where(i => i.Owner == ownerGuid && i.Warehouse != null && i.Warehouse.EngineId == engineId)
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

            NwItem? restored = await RestoreItemAsync(stored.ItemData, recipient).ConfigureAwait(false);
            if (restored is null)
            {
                continue;
            }

            NWScript.SetLocalString(restored, PlayerStallItemLocals.ConsignorPersonaId, persona.ToString());

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

    private static async Task<Storage> EnsureStorageAsync(
        PwEngineContext context,
        string? areaResRef,
        CancellationToken cancellationToken)
    {
        Guid engineId = ComputeStorageEngineId(areaResRef);

        Storage? existing = await context.Warehouses
            .FirstOrDefaultAsync(w => w.EngineId == engineId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            return existing;
        }

        Storage storage = new()
        {
            EngineId = engineId,
            Capacity = -1
        };

        await context.Warehouses.AddAsync(storage, cancellationToken).ConfigureAwait(false);
        return storage;
    }

    private static async Task<NwItem?> RestoreItemAsync(byte[] itemData, NwCreature owner)
    {
        await NwTask.SwitchToMainThread();

        if (owner is not { IsValid: true })
        {
            return null;
        }

        Location? location = owner.Location;
        if (location is null)
        {
            return null;
        }

        try
        {
            string jsonText = Encoding.UTF8.GetString(itemData);
            Json json = Json.Parse(jsonText);
            return json.ToNwObject<NwItem>(location, owner);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to restore stored stall item from reeve lockup.");
            return null;
        }
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
}
