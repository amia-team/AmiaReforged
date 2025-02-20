using Anvil.API;
using Anvil.Services;
using NWN.Core.NWNX;

namespace AmiaReforged.Classes.Rangers;

[ServiceBinding(typeof(FavoredEnemyService))]
public class FavoredEnemyService
{
     
    public FavoredEnemyService()
    {
        foreach (KeyValuePair<int,NwFeat> race in RacialTypesConst.FavoredEnemyMap)
        {
            RacePlugin.SetFavoredEnemyFeat(race.Key, race.Value.Id);
        }
    }
}