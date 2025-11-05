using System;
using System.Linq;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Database;

/// <summary>
/// Persistence boundary for player-level personas keyed by CD key.
/// </summary>
[ServiceBinding(typeof(IPersistentPlayerPersonaRepository))]
public sealed class PersistentPlayerPersonaRepository : IPersistentPlayerPersonaRepository
{
    private readonly PwContextFactory _factory;

    public PersistentPlayerPersonaRepository(PwContextFactory factory)
    {
        _factory = factory;
    }

    public PlayerPersonaRecord Upsert(string cdKey, string displayName, DateTime? observedUtc = null)
    {
        string normalizedCdKey = PersonaId.NormalizePlayerCdKey(cdKey);
        DateTime timestamp = observedUtc?.ToUniversalTime() ?? DateTime.UtcNow;

        using PwEngineContext context = _factory.CreateDbContext();

        PlayerPersonaRecord? record = context.PlayerPersonas
            .FirstOrDefault(p => p.CdKey == normalizedCdKey);

        if (record is null)
        {
            record = new PlayerPersonaRecord
            {
                CdKey = normalizedCdKey,
                DisplayName = displayName,
                PersonaIdString = PersonaId.FromPlayerCdKey(normalizedCdKey).ToString(),
                CreatedUtc = timestamp,
                UpdatedUtc = timestamp,
                LastSeenUtc = timestamp
            };

            context.PlayerPersonas.Add(record);
        }
        else
        {
            record.DisplayName = displayName;
            record.PersonaIdString ??= PersonaId.FromPlayerCdKey(normalizedCdKey).ToString();
            record.LastSeenUtc = timestamp;
            record.UpdatedUtc = timestamp;

            context.PlayerPersonas.Update(record);
        }

        context.SaveChanges();
        return record;
    }

    public PlayerPersonaRecord? GetByCdKey(string cdKey)
    {
        string normalizedCdKey = PersonaId.NormalizePlayerCdKey(cdKey);

        using PwEngineContext context = _factory.CreateDbContext();
        return context.PlayerPersonas.FirstOrDefault(p => p.CdKey == normalizedCdKey);
    }

    public void Touch(string cdKey, DateTime observedUtc)
    {
        string normalizedCdKey = PersonaId.NormalizePlayerCdKey(cdKey);
        DateTime timestamp = observedUtc.ToUniversalTime();

        using PwEngineContext context = _factory.CreateDbContext();
        PlayerPersonaRecord? record = context.PlayerPersonas.FirstOrDefault(p => p.CdKey == normalizedCdKey);
        if (record is null)
        {
            return;
        }

        record.LastSeenUtc = timestamp;
        record.UpdatedUtc = timestamp;

        context.PlayerPersonas.Update(record);
        context.SaveChanges();
    }
}

public interface IPersistentPlayerPersonaRepository
{
    PlayerPersonaRecord Upsert(string cdKey, string displayName, DateTime? observedUtc = null);

    PlayerPersonaRecord? GetByCdKey(string cdKey);

    void Touch(string cdKey, DateTime observedUtc);
}
