namespace AmiaReforged.Core.Models.Sailing.Ship.Crew;

/// <summary>
/// Represents the collection of crew members assigned to a ship and manages their roles.
/// </summary>
public sealed class ShipCrew
{
    private readonly List <ShipCrewMember> _members = [];

    /// <summary>
    /// A list of all crew members currently assigned to the ship.
    /// </summary>
    public IReadOnlyList<ShipCrewMember> Members => _members;

    /// <summary>
    /// Adds a new member to the crew and assigns them a role.
    /// Note: A ship can only have one Captain at a time.
    /// </summary>
    /// <param name="member">The crew member to add.</param>
    /// <param name="role">The role to assign to the new member.</param>
    public void AddMember(ShipCrewMember member, ShipCrewRole role)
    {
        if (_members.Any(m => m == member)
            || role == ShipCrewRole.Captain && _members.Any(m => m.Role == ShipCrewRole.Captain))
            return;

        member.Role = role;
        _members.Add(member);
    }

    /// <summary>
    /// Removes a member from the ship's crew.
    /// </summary>
    /// <param name="member">The member to remove.</param>
    public void RemoveMember(ShipCrewMember member) => _members.Remove(member);

    /// <summary>
    /// Changes the role of an existing crew member.
    /// </summary>
    /// <param name="member">The member whose role is being changed.</param>
    /// <param name="role">The new role to assign.</param>
    public void AssignRole(ShipCrewMember member, ShipCrewRole role)
    {
        if (role == ShipCrewRole.Captain && _members.Any(m => m.Role == ShipCrewRole.Captain))
            return;

        member.Role = role;
    }
}
