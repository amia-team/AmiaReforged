using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy;


[ServiceBinding(typeof(CoinhouseService))]
public class CoinhouseService(ICoinhouseRepository coinhouseRepository)
{

    public bool ItemsAreInHoldingForPlayer(CharacterId id)
    {
        CoinHouse? account =  coinhouseRepository.GetAccountFor(id.Value);
        if(account is null) return false;

        return true;
    }
}
