using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.Services;
using static AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping.JobSystemResRefConsts;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

/// <summary>
///     Maps legacy job system items to a material enum.
/// </summary>
[ServiceBinding(typeof(MaterialResRefMapper))]
public class MaterialResRefMapper : IMapAtoB<MaterialEnum, string>
{
    /// <summary>
    ///     Takes some resref and tries to resolve a material enum from it.
    /// </summary>
    /// <param name="resRef">The blueprint resRef of the job system item to map from</param>
    /// <returns>an enum used to identify the material of the job system item in the database</returns>
    public MaterialEnum MapFrom(string resRef)
    {
        return resRef switch
        {
            // TODO: Add more mappings...
            JsMetAdao => MaterialEnum.Adamantine,
            JsMetGolo => MaterialEnum.Gold,
            JsMetIroo => MaterialEnum.Iron,
            JsMetMito => MaterialEnum.Mithral,
            JsMetPlao => MaterialEnum.Platinum,
            JsMetSilo => MaterialEnum.Silver,
            JsAlchElea => MaterialEnum.Elemental_Air,
            JsAlchElee => MaterialEnum.Elemental_Earth,
            JsAlchElef => MaterialEnum.Elemental_Fire,
            JsAlchElew => MaterialEnum.Elemental_Water,
            JsBlaAdin => MaterialEnum.Adamantine,
            JsBlaSiin => MaterialEnum.Silver,
            JsBlaStin => MaterialEnum.Steel,
            JsBlaIrin => MaterialEnum.Iron,
            JsBlaPlin => MaterialEnum.Platinum,
            JsBlaGoin => MaterialEnum.Gold,
            JsBlaMiin => MaterialEnum.Mithral,
            _ => MaterialEnum.None
        };
    }
}
