namespace AmiaReforged.Core.Models.Sailing;

public class PhysicalShip
{
    public required string ShipName { get; set; }

    public required string PlaceableTag { get; set; }

    public string? PlaceableResRef { get; set; }

    public required string ExitPlaceableTag { get; set; }

    public required string DeckAreaResRef { get; set; }

    public required string CabinAreaResRef { get; set; }
}