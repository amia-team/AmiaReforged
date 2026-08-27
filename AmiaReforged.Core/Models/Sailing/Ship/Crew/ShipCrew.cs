using Anvil.API;

namespace AmiaReforged.Core.Models.Sailing.Ship.Crew;

public sealed class ShipCrew
{
    public class ShipCrewMember(NwCreature creature)
    {
        public ShipCrewRole Role { get; set; }
    }

    private readonly List <ShipCrewMember> _members = [];

    public IReadOnlyList<ShipCrewMember> Members => _members;

    public void AddMember(ShipCrewMember member, Types.ShipCrewRole role)
    {
        if (role == Types.ShipCrewRole.Captain && _members.Any(m => m.Role == ShipCrewRole.Captain))
            return;

        _members.Add(member);
    }

    public void RemoveMember(ShipCrewMember member)
        => _members.Remove(member);

    public void AssignRole(ShipCrewMember member, ShipCrewRole role)
    {
        if (role == ShipCrewRole.Captain && _members.Any(m => m.Role == ShipCrewRole.Captain))
            return;

        _members.First(m => m == member).Role = role;
    }
}
