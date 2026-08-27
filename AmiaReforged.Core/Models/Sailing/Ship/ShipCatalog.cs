namespace AmiaReforged.Core.Models.Sailing.Ship;

public class ShipCatalog
{
    public Ship GoldenGull { get; } = new(
        name: "Golden Gull",
        type: ShipType.Player,
        maximumHull: 100,
        position: new ShipPosition(
            AreaResRef: "ocean_01",
            X: 160.0f,
            Y: 80.0f));

    public Ship BlackPearl { get; } = new(
        name: "Black Pearl",
        type: ShipType.Player,
        maximumHull: 100,
        position: new ShipPosition(
            AreaResRef: "ocean_01",
            X: 120.0f,
            Y: 80.0f));

    public Ship SeaSprite { get; } = new(
        name: "Sea Sprite",
        type: ShipType.Player,
        maximumHull: 100,
        position: new ShipPosition(
            AreaResRef: "ocean_01",
            X: 80.0f,
            Y: 80.0f));
}
