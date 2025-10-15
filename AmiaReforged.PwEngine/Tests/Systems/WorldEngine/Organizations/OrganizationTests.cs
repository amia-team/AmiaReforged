using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;
using NUnit.Framework;
using Organization = AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Organization;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Organizations;

[TestFixture]
public class OrganizationTests
{
    private IOrganizationSystem _organizationSystem = null!;


    [SetUp]
    public void SetUp()
    {
        IOrganizationRepository inMemoryRepository = new InMemoryOrganizationRepository();
        _organizationSystem = new OrganizationSystem(inMemoryRepository);
    }


    [Test]
    public void Creates_Organization()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);

        Assert.That(org, Is.Not.Null);
    }

    [Test]
    public void Registers_Organization()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);

        SystemResponse response = _organizationSystem.Register(org);

        Assert.That(response.Result, Is.EqualTo(SystemResult.Success));
    }

    [Test]
    public void Fails_To_Register_Duplicate_Organization()
    {
        // Register a duplicate ID
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);

        SystemResponse response = _organizationSystem.Register(org);

        Assert.That(response.Result, Is.EqualTo(SystemResult.Success), response.Message);

        SystemResponse response1 = _organizationSystem.Register(org);

        Assert.That(response1.Result, Is.EqualTo(SystemResult.DuplicateEntry),
            "Expected org with same ID to be rejected.");

        // Register a duplicate name
        IOrganization org2 = Organization.CreateNew("test", "test", OrganizationType.Guild);

        SystemResponse response2 = _organizationSystem.Register(org2);

        Assert.That(response2.Result, Is.EqualTo(SystemResult.DuplicateEntry),
            "Expected org with same name to be rejected.");
    }

    [Test]
    public void Registers_With_Parent()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);

        _organizationSystem.Register(org);

        IOrganization childOrg = Organization.CreateNew("child", "child", OrganizationType.Guild, org.Id);

        SystemResponse response1 = _organizationSystem.Register(childOrg);

        Assert.That(response1.Result, Is.EqualTo(SystemResult.Success));
    }

    [Test]
    public void Can_Retrieve_Parent()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);
        _organizationSystem.Register(org);

        IOrganization childOrg = Organization.CreateNew("child", "child", OrganizationType.Guild, org.Id);
        SystemResponse response = _organizationSystem.Register(childOrg);

        Assert.That(response.Result, Is.EqualTo(SystemResult.Success));

        IOrganization? organization = _organizationSystem.ParentFor(childOrg);

        Assert.That(organization, Is.Not.Null);
    }

    [Test]
    public void Returns_All_Children()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);
        _organizationSystem.Register(org);

        IOrganization childOrg = Organization.CreateNew("child", "child", OrganizationType.Guild, org.Id);
        _organizationSystem.Register(childOrg);

        IOrganization grandChildOrg =
            Organization.CreateNew("grandchild", "grandchild", OrganizationType.Guild, childOrg.Id);
        _organizationSystem.Register(grandChildOrg);

        List<IOrganization> expected = [childOrg, grandChildOrg];
        expected.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        List<IOrganization> response = _organizationSystem.SubordinateOrganizationsFor(org);
        response.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        Assert.That(response, Is.EqualTo(expected));
    }

    [Test]
    public void Has_An_Inbox()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);

        IReadOnlyList<OrganizationRequest> list = org.GetInbox();

        Assert.That(list, Is.Not.Null);
    }

    [Test]
    public void Receives_Requests()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);

        _organizationSystem.Register(org);

        OrganizationRequest organizationRequest = new(Guid.NewGuid(), org.Id, OrganizationActionType.Join,
            "test");

        OrganizationResponse response = _organizationSystem.SendRequest(organizationRequest);

        Assert.That(response.Response, Is.EqualTo(OrganizationRequestResponse.Sent), response.Message);
        Assert.That(org.GetInbox(), Does.Contain(organizationRequest));
    }

    [Test]
    public void Gets_Organization_Relationship_Tree()
    {


    }

    [Test]
    public void Rejects_Banned_Characters()
    {
        IOrganization org = Organization.CreateNew("test", "test", OrganizationType.Guild);

        _organizationSystem.Register(org);

        Guid fakeId = Guid.NewGuid();
        _organizationSystem.BanCharacterFrom(fakeId, org);
    }
}
