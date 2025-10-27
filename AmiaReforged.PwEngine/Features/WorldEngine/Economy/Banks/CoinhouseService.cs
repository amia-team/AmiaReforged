using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Banks;


[ServiceBinding(typeof(CoinhouseService))]
public class CoinhouseService
{
    private readonly ICoinhouseRepository _coinHouses;
    private readonly IWarehouseRepository _warehouses;

    public CoinhouseService(ICoinhouseRepository coinHouses, IWarehouseRepository warehouses, SchedulerService scheduler)
    {
        _coinHouses = coinHouses;
        _warehouses = warehouses;

    }


    public bool ItemsAreInHoldingForPlayer(CharacterId id)
    {

        return false;
    }
}


