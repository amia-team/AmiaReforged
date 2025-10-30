using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

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
            Coinhouse = entity.CoinHouse?.ToDto()
        };
    }

    public static CoinHouseAccount ToEntity(this CoinhouseAccountDto dto)
    {
        return new CoinHouseAccount
        {
            Id = dto.Id,
            Debit = dto.Debit,
            Credit = dto.Credit,
            CoinHouseId = dto.CoinHouseId,
            OpenedAt = dto.OpenedAt,
            LastAccessedAt = dto.LastAccessedAt
        };
    }

    public static void UpdateFrom(this CoinHouseAccount entity, CoinhouseAccountDto dto)
    {
        entity.Debit = dto.Debit;
        entity.Credit = dto.Credit;
        entity.CoinHouseId = dto.CoinHouseId;
        entity.OpenedAt = dto.OpenedAt;
        entity.LastAccessedAt = dto.LastAccessedAt;
    }
}
