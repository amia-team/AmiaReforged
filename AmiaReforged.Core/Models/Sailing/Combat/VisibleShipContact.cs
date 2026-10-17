namespace AmiaReforged.Core.Models.Sailing;

public sealed class VisibleShipContact
{
    public required ShipState Ship { get; init; }

    public float Distance { get; init; }

    public bool IsHorizonContact { get; init; }
}