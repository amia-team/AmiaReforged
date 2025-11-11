namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Queries;

/// <summary>
/// Query to retrieve a rentable property by its associated POI ResRef.
/// </summary>
public sealed record GetPropertyByPoiQuery(string PoiResRef);
