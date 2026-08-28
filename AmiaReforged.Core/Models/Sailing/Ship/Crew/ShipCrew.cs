namespace AmiaReforged.Core.Models.Sailing.Ship.Crew;

public sealed class ShipCrew
{
    private readonly List <ShipCrewMember> _members = [];

    public IReadOnlyList<ShipCrewMember> Members => _members;

    public void AddMember(ShipCrewMember member, ShipCrewRole role)
    {
        if (_members.Any(m => m == member)
            || role == ShipCrewRole.Captain && _members.Any(m => m.Role == ShipCrewRole.Captain))
            return;

        member.Role = role;
        _members.Add(member);
    }

    public void RemoveMember(ShipCrewMember member) => _members.Remove(member);

    public void AssignRole(ShipCrewMember member, ShipCrewRole role)
    {
        if (role == ShipCrewRole.Captain && _members.Any(m => m.Role == ShipCrewRole.Captain))
            return;

        member.Role = role;
    }
}
