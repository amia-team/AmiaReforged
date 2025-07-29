namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage.Mapping;

/// <summary>
///     Interface for mapping from one object to another. Bidirectional, meaning it can map from A to B and B to A.
/// </summary>
/// <typeparam name="TA">Assumed to be any type 'A' that is sent to (can be mapped to) 'B'</typeparam>
/// <typeparam name="TB">Assumed to be any type 'B' that was sent from A or can be sent to A</typeparam>
public interface IMappingService<TA, TB> : IMapAtoB<TA, TB>, IMapBtoA<TA, TB>
{

}

public interface IMapAtoB<out TA, in TB>
{
    /// <summary>
    ///     Maps to A given B
    /// </summary>
    /// <param name="b">The object 'B' to map onto A</param>
    /// <returns></returns>
    public TA MapFrom(TB b);
}

public interface IMapBtoA<in TA, out TB>
{
    /// <summary>
    ///     Maps from A given B
    /// </summary>
    /// <param name="a">The object 'A' to map onto B</param>
    /// <returns></returns>
    public TB? MapTo(TA a);
}