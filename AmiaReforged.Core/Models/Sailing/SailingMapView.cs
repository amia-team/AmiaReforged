namespace AmiaReforged.Core.Models.Sailing;

/// <summary>Only state visible on this viewer's map participates in its refresh decision.</summary>
public sealed record SailingMapView(string ShipId, string Area, float X, float Y, Heading Heading,
    string Sprite, long DiscoveryRevision, SailingMapContact[] Contacts)
{
    public bool Matches(SailingMapView other) => ShipId == other.ShipId && Area == other.Area &&
        X == other.X && Y == other.Y && Heading == other.Heading && Sprite == other.Sprite &&
        DiscoveryRevision == other.DiscoveryRevision && Contacts.SequenceEqual(other.Contacts);
}
public sealed record SailingMapContact(string ShipId, float X, float Y, Heading Heading, string Sprite);
