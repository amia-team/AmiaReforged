using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Organizations;

public interface IOrganizationSystem
{
    OrganizationResponse SendRequest(OrganizationRequest request);
    SystemResponse Register(IOrganization organization);
    IOrganization? ParentFor(IOrganization childOrgId);
    List<IOrganization> SubordinateOrganizationsFor(IOrganization org);
}

public interface IOrganization
{
    public string Name { get; init; }
    public string Description { get; init; }
    public OrganizationType Type { get; init; }
    public OrganizationId Id { get; init; }
    public OrganizationId? ParentOrganization { get; init; }
}

public record OrganizationRequest(
    Guid CharacterId,
    Guid OrganizationId,
    OrganizationActionType Action,
    string? Message = null);

public record OrganizationResponse(Guid CharacterId, Guid OrganizationId, OrganizationRequestResponse Response);

public enum OrganizationActionType
{
    Join,
    Leave,
    Promote,
    Demote,
    Withdraw,
    Kick
}

public enum OrganizationRequestResponse
{
    Failed,
    Blocked,
    Sent
}

public enum SystemResult
{
    Success,
    Failed,
    DuplicateEntry
}

public record SystemResponse(SystemResult Result, string? Message = null);
