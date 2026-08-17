namespace AmiaReforged.Core.Models.Sailing;

public class ShipCrewMember
{
    public required string PlayerName { get; set; }

    public ShipCrewRole Role { get; set; }
}