using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Personas;

[TestFixture]
public class PersonaTests
{
    [Test]
    public void CharacterPersona_Create_ValidInput_CreatesPersona()
    {
        // Arrange
        var characterId = CharacterId.New();
        var name = "Aldric the Brave";

        // Act
        var persona = CharacterPersona.Create(characterId, name);

        // Assert
        Assert.That(persona.CharacterId, Is.EqualTo(characterId));
        Assert.That(persona.DisplayName, Is.EqualTo(name));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(persona.Id.Type, Is.EqualTo(PersonaType.Character));
        Assert.That(persona.Id.Value, Is.EqualTo(characterId.Value.ToString()));
    }

    [Test]
    public void CharacterPersona_Create_EmptyName_ThrowsArgumentException()
    {
        // Arrange
        var characterId = CharacterId.New();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => CharacterPersona.Create(characterId, ""));
        Assert.Throws<ArgumentException>(() => CharacterPersona.Create(characterId, "   "));
    }

    [Test]
    public void OrganizationPersona_Create_ValidInput_CreatesPersona()
    {
        // Arrange
        var orgId = OrganizationId.New();
        var name = "Merchants Guild";

        // Act
        var persona = OrganizationPersona.Create(orgId, name);

        // Assert
        Assert.That(persona.OrganizationId, Is.EqualTo(orgId));
        Assert.That(persona.DisplayName, Is.EqualTo(name));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.Organization));
        Assert.That(persona.Id.Type, Is.EqualTo(PersonaType.Organization));
    }

    [Test]
    public void CoinhousePersona_Create_ValidInput_CreatesPersona()
    {
        // Arrange
        var tag = new CoinhouseTag("cordor-bank");
        var settlement = SettlementId.Parse(1);
        var name = "Cordor Central Bank";

        // Act
        var persona = CoinhousePersona.Create(tag, settlement, name);

        // Assert
        Assert.That(persona.Tag, Is.EqualTo(tag));
        Assert.That(persona.Settlement, Is.EqualTo(settlement));
        Assert.That(persona.DisplayName, Is.EqualTo(name));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.Coinhouse));
        Assert.That(persona.Id.Type, Is.EqualTo(PersonaType.Coinhouse));
    }

    [Test]
    public void GovernmentPersona_Create_ValidInput_CreatesPersona()
    {
        // Arrange
        var govId = GovernmentId.New();
        var settlement = SettlementId.Parse(1);
        var name = "City of Cordor";

        // Act
        var persona = GovernmentPersona.Create(govId, settlement, name);

        // Assert
        Assert.That(persona.GovernmentId, Is.EqualTo(govId));
        Assert.That(persona.Settlement, Is.EqualTo(settlement));
        Assert.That(persona.DisplayName, Is.EqualTo(name));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.Government));
        Assert.That(persona.Id.Type, Is.EqualTo(PersonaType.Government));
    }

    [Test]
    public void SystemPersona_Create_ValidInput_CreatesPersona()
    {
        // Arrange
        var processName = "TaxCollector";
        var displayName = "Automated Tax Collection";

        // Act
        var persona = SystemPersona.Create(processName, displayName);

        // Assert
        Assert.That(persona.ProcessName, Is.EqualTo(processName));
        Assert.That(persona.DisplayName, Is.EqualTo(displayName));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.SystemProcess));
        Assert.That(persona.Id.Type, Is.EqualTo(PersonaType.SystemProcess));
    }

    [Test]
    public void SystemPersona_Create_NoDisplayName_UsesDefault()
    {
        // Act
        var persona = SystemPersona.Create("TaxCollector");

        // Assert
        Assert.That(persona.DisplayName, Is.EqualTo("System: TaxCollector"));
    }

    [Test]
    public void TestHelpers_CreateCharacterPersona_Works()
    {
        // Act
        var persona = PersonaTestHelpers.CreateCharacterPersona("TestName");

        // Assert
        Assert.That(persona.DisplayName, Is.EqualTo("TestName"));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.Character));
    }

    [Test]
    public void TestHelpers_CreateOrganizationPersona_Works()
    {
        // Act
        var persona = PersonaTestHelpers.CreateOrganizationPersona("TestOrg");

        // Assert
        Assert.That(persona.DisplayName, Is.EqualTo("TestOrg"));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.Organization));
    }

    [Test]
    public void TestHelpers_CreateCoinhousePersona_Works()
    {
        // Act
        var persona = PersonaTestHelpers.CreateCoinhousePersona("test-bank", 5, "Test Bank");

        // Assert
        Assert.That(persona.DisplayName, Is.EqualTo("Test Bank"));
        Assert.That(persona.Settlement.Value, Is.EqualTo(5));
        Assert.That(persona.Type, Is.EqualTo(PersonaType.Coinhouse));
    }
}

