using AmiaReforged.PwEngine.Database.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Organizations;

public class Organization : IOrganization
{
    public List<OrganizationRequest> Inbox { get; init; } = [];
    public string Name { get; init; }
    public string Description { get; init; }
    public OrganizationType Type { get; init; }
    public required OrganizationId Id { get; init; }

    public OrganizationId? ParentOrganization { get; init; } = null;

    /// <summary>
    /// Force the use of the factory method.
    /// </summary>
    private Organization(string name, string description, OrganizationType type)
    {
        Name = name;
        Description = description;
        Type = type;
    }

    public IReadOnlyList<OrganizationRequest> GetInbox()
    {
        return Inbox;
    }

    public void AddToInbox(OrganizationRequest request)
    {
        Inbox.Add(request);
        OnRequestMade?.Invoke();
    }

    public event IOrganization.RequestMade? OnRequestMade;

    public static IOrganization CreateNew(string test, string description, OrganizationType type,
        OrganizationId? parent = null)
    {
        return new Organization(test, description, type)
        {
            Id = new OrganizationId(Guid.NewGuid()),
            ParentOrganization = parent
        };
    }
}

public record OrganizationId(Guid Value);
