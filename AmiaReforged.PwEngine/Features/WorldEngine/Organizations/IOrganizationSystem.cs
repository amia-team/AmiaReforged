using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

public interface IOrganizationSystem
{
    OrganizationResponse SendRequest(OrganizationRequest request);
    SystemResponse Register(IOrganization organization);
    IOrganization? ParentFor(IOrganization childOrgId);
    List<IOrganization> SubordinateOrganizationsFor(IOrganization org);
    void BanCharacterFrom(Guid fakeId, IOrganization org);
}

public interface IOrganization
{
    public string Name { get; init; }
    public string Description { get; init; }
    public OrganizationType Type { get; init; }
    public OrganizationId Id { get; init; }
    public OrganizationId? ParentOrganization { get; init; }

    public delegate OrganizationRequest RequestMade();

    public event RequestMade? OnRequestMade;
    IReadOnlyList<OrganizationRequest> GetInbox();
    void AddToInbox(OrganizationRequest request);
}

public record OrganizationRequest(
    Guid CharacterId,
    OrganizationId OrganizationId,
    OrganizationActionType Action,
    string? Message = null);

public record OrganizationResponse(OrganizationRequestResponse Response, string Message)
{

    public static OrganizationResponse NotFound() => new(OrganizationRequestResponse.Failed, "Organization not found");
    public static OrganizationResponse Blocked() => new(OrganizationRequestResponse.Blocked, "Organization is blocked");
    public static OrganizationResponse Sent() => new(OrganizationRequestResponse.Sent, "Request sent");
};

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
