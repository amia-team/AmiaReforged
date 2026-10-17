namespace AmiaReforged.Core.Models.Sailing;

public sealed class OceanContact
{
    public string Id { get; init; } = "";

    public EncounterType Type { get; init; }

    public string Name { get; init; } = "";

    public string AreaResRef { get; init; } = "";

    public float X { get; set; }

    public float Y { get; set; }

    public float Z { get; set; }

    public bool Visible { get; set; } = true;

    public bool Discovered { get; set; }

    public bool Spawned { get; set; }

    public bool ConvertedToShip { get; set; }

    public string ShipTag { get; init; } = "";

    public string ShipResRef { get; init; } = "";
}