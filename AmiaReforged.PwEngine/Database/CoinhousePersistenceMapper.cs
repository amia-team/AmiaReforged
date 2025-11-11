using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Database;

internal static class CoinhousePersistenceMapper
{
    public static CoinhouseDto ToDto(this CoinHouse entity)
    {
        PersonaId persona = entity.PersonaIdString != null
            ? PersonaId.Parse(entity.PersonaIdString)
            : PersonaId.FromCoinhouse(new CoinhouseTag(entity.Tag));

        return new CoinhouseDto
        {
            Id = entity.Id,
            Tag = new CoinhouseTag(entity.Tag),
            Settlement = entity.Settlement,
            EngineId = entity.EngineId,
            Persona = persona
        };
    }

    public static CoinhouseAccountDto ToDto(this CoinHouseAccount entity)
    {
        return new CoinhouseAccountDto
        {
            Id = entity.Id,
            Debit = entity.Debit,
            Credit = entity.Credit,
            CoinHouseId = entity.CoinHouseId,
            OpenedAt = entity.OpenedAt,
            LastAccessedAt = entity.LastAccessedAt,
            Coinhouse = entity.CoinHouse?.ToDto(),
            Holders = entity.AccountHolders?.Select(static h => h.ToDto()).ToArray() ?? Array.Empty<CoinhouseAccountHolderDto>()
        };
    }

    public static CoinHouseAccount ToEntity(this CoinhouseAccountDto dto)
    {
        List<CoinHouseAccountHolder>? holders = dto.Holders.Count == 0
            ? null
            : dto.Holders.Select(h => h.ToEntity(dto.Id)).ToList();

        CoinHouseAccount account = new CoinHouseAccount
        {
            Id = dto.Id,
            Debit = dto.Debit,
            Credit = dto.Credit,
            CoinHouseId = dto.CoinHouseId,
            OpenedAt = dto.OpenedAt,
            LastAccessedAt = dto.LastAccessedAt,
            AccountHolders = holders
        };

        return account;
    }

    public static void UpdateFrom(this CoinHouseAccount entity, CoinhouseAccountDto dto)
    {
        entity.Debit = dto.Debit;
        entity.Credit = dto.Credit;
        entity.CoinHouseId = dto.CoinHouseId;
        entity.OpenedAt = dto.OpenedAt;
        entity.LastAccessedAt = dto.LastAccessedAt;
    }

    internal static CoinhouseAccountHolderDto ToDto(this CoinHouseAccountHolder entity)
    {
        return new CoinhouseAccountHolderDto
        {
            Id = entity.Id,
            HolderId = entity.HolderId,
            Type = entity.Type,
            Role = entity.Role,
            FirstName = entity.FirstName,
            LastName = entity.LastName
        };
    }

    internal static CoinHouseAccountHolder ToEntity(this CoinhouseAccountHolderDto dto, Guid accountId)
    {
        return new CoinHouseAccountHolder
        {
            Id = dto.Id ?? 0,
            AccountId = accountId,
            HolderId = dto.HolderId,
            Type = dto.Type,
            Role = dto.Role,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };
    }
}
