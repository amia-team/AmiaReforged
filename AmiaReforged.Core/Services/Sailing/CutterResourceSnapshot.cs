namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Native observations collected on the main thread, validated without accessing NWN objects.</summary>
public sealed record CutterResourceSnapshot
{
    public int DeckCount { get; init; }
    public int CabinCount { get; init; }
    public int RepresentationCount { get; init; }
    public bool RepresentationInShipArea { get; init; }
    public int HelmCount { get; init; }
    public bool HelmInInterior { get; init; }
    public int ExitCount { get; init; }
    public int DeckExitCount { get; init; }
    public int CabinExitCount { get; init; }
    public int DeckSpawnCount { get; init; }
    public bool SpawnLocationValid { get; init; }
    public bool UniqueInteriors { get; init; }

    public IReadOnlyList<string> Failures(RentalVesselOption slot, string oceanArea)
    {
        List<string> failures = [];
        bool cabin = !string.IsNullOrWhiteSpace(slot.CabinAreaResRef);
        if (DeckCount != 1) failures.Add($"deck area ResRef '{slot.DeckAreaResRef}': expected 1, found {DeckCount}");
        if (cabin && CabinCount != 1) failures.Add($"cabin area ResRef '{slot.CabinAreaResRef}': expected 1, found {CabinCount}");
        if (RepresentationCount != 1 || !RepresentationInShipArea)
            failures.Add($"ocean placeable tag '{slot.PlaceableTag}': expected 1 in '{oceanArea}', found {RepresentationCount}; correct area={RepresentationInShipArea}");
        if (HelmCount != 1 || !HelmInInterior)
            failures.Add($"helm placeable tag '{slot.HelmTag}': expected 1 on its interior, found {HelmCount}; correct area={HelmInInterior}");
        if (ExitCount != (cabin ? 2 : 1) || DeckExitCount != 1 || cabin && CabinExitCount != 1)
            failures.Add($"exit placeable tag '{slot.ExitTag}': expected 1 on deck{(cabin ? " and 1 in cabin" : "")}, found total={ExitCount}, deck={DeckExitCount}, cabin={CabinExitCount}");
        if (DeckSpawnCount != 1 || !SpawnLocationValid)
            failures.Add($"waypoint tag 'SAILING_DECK_SPAWN' on '{slot.DeckAreaResRef}': expected 1 valid destination, found {DeckSpawnCount}; valid location={SpawnLocationValid}");
        if (!UniqueInteriors) failures.Add($"interior '{slot.DeckAreaResRef}' is assigned to another ship");
        return failures;
    }
}
