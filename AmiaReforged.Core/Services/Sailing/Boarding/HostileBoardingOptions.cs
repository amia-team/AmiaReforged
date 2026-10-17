namespace AmiaReforged.Core.Services.Sailing;

public sealed class HostileNpcDeckOption
{
    public string ShipId { get; set; } = "";
    public string DeckAreaResRef { get; set; } = "";
    public string BoardingWaypointTag { get; set; } = "SAILING_DECK_SPAWN";
    public string DefenderWaypointTag { get; set; } = "";
    // Must be supplied from a verified module/HAK UTC. No fabricated blueprint default.
    public string DefenderResRef { get; set; } = "";
    public int DefenderCount { get; set; } = 1;
}

public sealed class HostileBoardingOptions
{
    // Asset readiness additionally gates each target, even when this switch is enabled.
    public bool Enabled { get; set; }
    public float Range { get; set; } = 5;
    public float MaximumHullFraction { get; set; } = 0.5f;
    public float MaximumMovementPerTick { get; set; }
    public int MaximumDefenders { get; set; } = 4;
    public int MaximumBoarders { get; set; } = 4;
    public string RecoveryWaypointTag { get; set; } = "";
    public List<HostileNpcDeckOption> NpcDecks { get; set; } = [];

    public void Validate()
    {
        if (!float.IsFinite(Range) || Range is <= 0 or > ShipEncounterService.EncounterDistance ||
            !float.IsFinite(MaximumHullFraction) || MaximumHullFraction is <= 0 or > 1 ||
            !float.IsFinite(MaximumMovementPerTick) || MaximumMovementPerTick is < 0 or > 5 ||
            MaximumDefenders is < 1 or > 8 || MaximumBoarders is < 1 or > 8 || NpcDecks == null ||
            NpcDecks.Any(p => p == null) || NpcDecks.GroupBy(p => p.ShipId, StringComparer.Ordinal).Any(g => g.Count() != 1) ||
            NpcDecks.GroupBy(p => p.DeckAreaResRef, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() != 1))
            throw new ArgumentException("Invalid hostile boarding limits or duplicate NPC decks.");
        foreach (var deck in NpcDecks)
            if (new[] { deck.ShipId, deck.DeckAreaResRef, deck.BoardingWaypointTag, deck.DefenderWaypointTag, deck.DefenderResRef }
                .Any(string.IsNullOrWhiteSpace) || deck.DefenderResRef.Length > 16 || deck.DefenderCount < 1 || deck.DefenderCount > MaximumDefenders)
                throw new ArgumentException("Hostile boarding requires explicit deck, spawn and verified defender mappings.");
    }
}
