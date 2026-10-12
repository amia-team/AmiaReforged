using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(OceanContactService))]
public sealed class OceanContactService
{
    public const float RevealRange = 40.0f;
    public const float DiscoveryRange = 30.0f;
    public const float SpawnRange = 15.0f;
    public const float AttackRange = 10.0f;
    public const float BoardRange = 5.0f;

    private readonly List<OceanContact> contacts =
    [
        // Activate the existing Black Fang definition. Its representation is
        // optional; the configured ocean/contact position already belongs to
        // the sailing world, so pirate combat also works with an abstract ship.
        new OceanContact
        {
            Id="pirate_001",
            Name="Black Fang",
            Type=EncounterType.Pirate,
            AreaResRef="ocean_002",
            X=120f,
            Y=100f,
           ShipTag="pirate_black_fang",
           ShipResRef="pirate_brig"
         }
    ];

    public IEnumerable<OceanContact> GetContacts(
        string areaResRef)
    {
        return contacts.Where(
            c => string.Equals(
                c.AreaResRef,
                areaResRef,
                StringComparison.OrdinalIgnoreCase)).ToArray();
    }

    public void RegisterContact(OceanContact contact)
    {
        if (string.IsNullOrWhiteSpace(contact.Id) || string.IsNullOrWhiteSpace(contact.Name) ||
            string.IsNullOrWhiteSpace(contact.AreaResRef) || !Enum.IsDefined(contact.Type) ||
            !float.IsFinite(contact.X) || !float.IsFinite(contact.Y) || !float.IsFinite(contact.Z))
            throw new ArgumentException("Invalid ocean contact.", nameof(contact));
        if (contacts.Any(c => c.Id == contact.Id)) throw new ArgumentException("Ocean contact identity already registered.", nameof(contact));
        contacts.Add(contact);
    }

    public void ReconcileRestoredShips(IEnumerable<ShipState> ships)
    {
        HashSet<string> identities = ships.Select(ship => ship.ShipId).ToHashSet(StringComparer.Ordinal);
        foreach (OceanContact contact in contacts.Where(contact => identities.Contains(contact.Id)).ToArray())
        {
            contact.Spawned = contact.ConvertedToShip = true;
            contacts.Remove(contact);
        }
    }

    public IEnumerable<OceanContact> GetVisibleContacts(
        ShipState ship)
    {
        if (!ShipActionRules.IsActive(ship)) yield break;
        foreach (OceanContact contact
            in GetContacts(ship.AreaResRef).Where(c => c.Visible && !c.ConvertedToShip))
        {
            float dx = contact.X - ship.X;
            float dy = contact.Y - ship.Y;

            float distance =
                MathF.Sqrt(dx * dx + dy * dy);

            if (distance <= RevealRange)
            {
                yield return contact;
            }
        }
    }

public OceanContact? GetClosestContact(
    ShipState ship)
{
    return GetVisibleContacts(ship)
        .OrderBy(c =>
        {
            float dx = c.X - ship.X;
            float dy = c.Y - ship.Y;
            return dx * dx + dy * dy;
        })
        .FirstOrDefault();
}

public void RemoveContact(
    OceanContact contact)
{
    contacts.Remove(contact);
}


}
