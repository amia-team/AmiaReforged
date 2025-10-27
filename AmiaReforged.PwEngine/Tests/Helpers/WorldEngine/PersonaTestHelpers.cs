using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;

/// <summary>
/// Test helper factory methods for creating test personas.
/// </summary>
public static class PersonaTestHelpers
{
    /// <summary>
    /// Creates a test character persona with a random GUID.
    /// </summary>
    public static CharacterPersona CreateCharacterPersona(string? name = null)
    {
        var characterId = CharacterId.New();
        return CharacterPersona.Create(characterId, name ?? $"TestCharacter-{characterId.Value:N}");
    }

    /// <summary>
    /// Creates a test character persona with a specific ID.
    /// </summary>
    public static CharacterPersona CreateCharacterPersona(CharacterId characterId, string? name = null)
    {
        return CharacterPersona.Create(characterId, name ?? $"TestCharacter-{characterId.Value:N}");
    }

    /// <summary>
    /// Creates a test organization persona with a random GUID.
    /// </summary>
    public static OrganizationPersona CreateOrganizationPersona(string? name = null)
    {
        var orgId = OrganizationId.New();
        return OrganizationPersona.Create(orgId, name ?? $"TestOrg-{orgId.Value:N}");
    }

    /// <summary>
    /// Creates a test organization persona with a specific ID.
    /// </summary>
    public static OrganizationPersona CreateOrganizationPersona(OrganizationId orgId, string? name = null)
    {
        return OrganizationPersona.Create(orgId, name ?? $"TestOrg-{orgId.Value:N}");
    }

    /// <summary>
    /// Creates a test coinhouse persona.
    /// </summary>
    public static CoinhousePersona CreateCoinhousePersona(string? tag = null, int settlementId = 1, string? name = null)
    {
        var coinhouseTag = new CoinhouseTag(tag ?? "test-coinhouse");
        var settlement = SettlementId.Parse(settlementId);
        return CoinhousePersona.Create(coinhouseTag, settlement, name ?? $"Test Coinhouse {tag ?? "default"}");
    }

    /// <summary>
    /// Creates a test government persona.
    /// </summary>
    public static GovernmentPersona CreateGovernmentPersona(int settlementId = 1, string? name = null)
    {
        var govId = GovernmentId.New();
        var settlement = SettlementId.Parse(settlementId);
        return GovernmentPersona.Create(govId, settlement, name ?? $"Test Government {settlementId}");
    }

    /// <summary>
    /// Creates a test government persona with a specific ID.
    /// </summary>
    public static GovernmentPersona CreateGovernmentPersona(GovernmentId govId, int settlementId = 1, string? name = null)
    {
        var settlement = SettlementId.Parse(settlementId);
        return GovernmentPersona.Create(govId, settlement, name ?? $"Test Government {settlementId}");
    }

    /// <summary>
    /// Creates a test system persona for automated processes.
    /// </summary>
    public static SystemPersona CreateSystemPersona(string? processName = null, string? displayName = null)
    {
        return SystemPersona.Create(processName ?? "TestProcess", displayName);
    }
}

