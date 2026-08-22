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
      /*  new OceanContact
        {
            Id="pirate_001",
            Name="Black Fang",
            Type=EncounterType.Pirate,
            AreaResRef="ocean_002",
            X=120f,
            Y=100f,
           ShipTag="pirate_black_fang",
           ShipResRef="pirate_brig"
         }*/
    ];

    public IEnumerable<OceanContact> GetContacts(
        string areaResRef)
    {
        return contacts.Where(
            c => string.Equals(
                c.AreaResRef,
                areaResRef,
                StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<OceanContact> GetVisibleContacts(
        ShipState ship)
    {
        foreach (OceanContact contact
            in GetContacts(ship.AreaResRef))
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