using System;
using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations;

public class OrganizationSystem(IOrganizationRepository organizations) : IOrganizationSystem
{
    public SystemResponse Register(IOrganization organization)
    {
        bool exists = organizations.GetAll().Any(o => o.Id == organization.Id || o.Name == organization.Name);
        if (exists)
        {
            return new SystemResponse(SystemResult.DuplicateEntry, "Organization already exists");
        }

        organizations.Add(organization);

        return new SystemResponse(SystemResult.Success);
    }

    public OrganizationResponse SendRequest(OrganizationRequest request)
    {
        IOrganization? org = organizations.GetById(request.OrganizationId);

        org?.AddToInbox(request);

        return org is null ? OrganizationResponse.NotFound() : OrganizationResponse.Sent();
    }

    public List<IOrganization> SubordinateOrganizationsFor(IOrganization org)
    {
        List<IOrganization> children = [];

        IOrganization? current = organizations
            .GetAll()
            .FirstOrDefault(o => o.ParentOrganization == org.Id);
        while (current is not null)
        {
            children.Add(current);

            current = organizations
                .GetAll()
                .FirstOrDefault(o => o.ParentOrganization == current.Id);
        }

        return children;
    }

    public void BanCharacterFrom(Guid fakeId, IOrganization org)
    {
    }

    public IOrganization? ParentFor(IOrganization organization)
    {
        return organizations
            .GetAll()
            .FirstOrDefault(p => p.Id == organization.ParentOrganization);
    }
}
