using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Database;

/// <summary>
/// Repository implementation for looking up personas across different entity types.
/// Uses EF Core to query the appropriate table based on PersonaType.
/// </summary>
[ServiceBinding(typeof(IPersonaRepository))]
public class PersistentPersonaRepository(PwContextFactory factory) : IPersonaRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public bool TryGetPersona(PersonaId personaId, out Persona? persona)
    {
        persona = null;

        try
        {
            using var ctx = factory.CreateDbContext();

            persona = personaId.Type switch
            {
                PersonaType.Character => ResolveCharacter(ctx, personaId),
                PersonaType.Organization => ResolveOrganization(ctx, personaId),
                PersonaType.Coinhouse => ResolveCoinhouse(ctx, personaId),
                PersonaType.Government => null, // TODO: Implement when Government entities exist
                PersonaType.Warehouse => null, // TODO: Implement when Warehouse entities exist
                PersonaType.SystemProcess => ResolveSystemProcess(personaId),
                _ => null
            };

            return persona != null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error resolving PersonaId: {personaId}");
            return false;
        }
    }

    public Persona GetPersona(PersonaId personaId)
    {
        if (!TryGetPersona(personaId, out var persona) || persona == null)
        {
            throw new InvalidOperationException($"Persona not found: {personaId}");
        }

        return persona;
    }

    public bool Exists(PersonaId personaId)
    {
        try
        {
            using var ctx = factory.CreateDbContext();

            return personaId.Type switch
            {
                PersonaType.Character => ExistsCharacter(ctx, personaId),
                PersonaType.Organization => ExistsOrganization(ctx, personaId),
                PersonaType.Coinhouse => ExistsCoinhouse(ctx, personaId),
                PersonaType.Government => false, // TODO: Implement when Government entities exist
                PersonaType.Warehouse => false, // TODO: Implement when Warehouse entities exist
                PersonaType.SystemProcess => true, // System processes always "exist"
                _ => false
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error checking existence of PersonaId: {personaId}");
            return false;
        }
    }

    public string? GetDisplayName(PersonaId personaId)
    {
        try
        {
            using var ctx = factory.CreateDbContext();

            return personaId.Type switch
            {
                PersonaType.Character => GetCharacterDisplayName(ctx, personaId),
                PersonaType.Organization => GetOrganizationDisplayName(ctx, personaId),
                PersonaType.Coinhouse => GetCoinhouseDisplayName(ctx, personaId),
                PersonaType.Government => null, // TODO: Implement when Government entities exist
                PersonaType.Warehouse => null, // TODO: Implement when Warehouse entities exist
                PersonaType.SystemProcess => $"System: {personaId.Value}",
                _ => null
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error getting display name for PersonaId: {personaId}");
            return null;
        }
    }

    public Dictionary<PersonaId, Persona> GetPersonas(IEnumerable<PersonaId> personaIds)
    {
        var result = new Dictionary<PersonaId, Persona>();
        var personaIdList = personaIds.ToList();

        if (personaIdList.Count == 0)
            return result;

        try
        {
            using var ctx = factory.CreateDbContext();

            // Group by type for efficient querying
            var groupedByType = personaIdList.GroupBy(p => p.Type);

            foreach (var group in groupedByType)
            {
                switch (group.Key)
                {
                    case PersonaType.Character:
                        ResolveCharacters(ctx, group.ToList(), result);
                        break;
                    case PersonaType.Organization:
                        ResolveOrganizations(ctx, group.ToList(), result);
                        break;
                    case PersonaType.Coinhouse:
                        ResolveCoinhouses(ctx, group.ToList(), result);
                        break;
                    case PersonaType.SystemProcess:
                        ResolveSystemProcesses(group.ToList(), result);
                        break;
                    // TODO: Add Government and Warehouse when implemented
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resolving multiple PersonaIds");
        }

        return result;
    }

    #region Character Resolution

    private CharacterPersona? ResolveCharacter(PwEngineContext ctx, PersonaId personaId)
    {
        if (!Guid.TryParse(personaId.Value, out var guid))
            return null;

        var character = ctx.Characters.FirstOrDefault(c => c.Id == guid);
        if (character == null)
            return null;

        return CharacterPersona.Create(
            CharacterId.From(character.Id),
            character.FullName
        );
    }

    private bool ExistsCharacter(PwEngineContext ctx, PersonaId personaId)
    {
        if (!Guid.TryParse(personaId.Value, out var guid))
            return false;

        return ctx.Characters.Any(c => c.Id == guid);
    }

    private string? GetCharacterDisplayName(PwEngineContext ctx, PersonaId personaId)
    {
        if (!Guid.TryParse(personaId.Value, out var guid))
            return null;

        return ctx.Characters
            .Where(c => c.Id == guid)
            .Select(c => c.FirstName + " " + c.LastName)
            .FirstOrDefault();
    }

    private void ResolveCharacters(PwEngineContext ctx, List<PersonaId> personaIds, Dictionary<PersonaId, Persona> result)
    {
        var guids = personaIds
            .Select(p => Guid.TryParse(p.Value, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        if (guids.Count == 0)
            return;

        var characters = ctx.Characters
            .Where(c => guids.Contains(c.Id))
            .ToList();

        foreach (var character in characters)
        {
            var personaId = PersonaId.FromCharacter(CharacterId.From(character.Id));
            var persona = CharacterPersona.Create(
                CharacterId.From(character.Id),
                character.FullName
            );
            result[personaId] = persona;
        }
    }

    #endregion

    #region Organization Resolution

    private OrganizationPersona? ResolveOrganization(PwEngineContext ctx, PersonaId personaId)
    {
        if (!Guid.TryParse(personaId.Value, out var guid))
            return null;

        var org = ctx.Organizations.FirstOrDefault(o => o.Id == guid);
        if (org == null)
            return null;

        return OrganizationPersona.Create(
            OrganizationId.From(org.Id),
            org.Name
        );
    }

    private bool ExistsOrganization(PwEngineContext ctx, PersonaId personaId)
    {
        if (!Guid.TryParse(personaId.Value, out var guid))
            return false;

        return ctx.Organizations.Any(o => o.Id == guid);
    }

    private string? GetOrganizationDisplayName(PwEngineContext ctx, PersonaId personaId)
    {
        if (!Guid.TryParse(personaId.Value, out var guid))
            return null;

        return ctx.Organizations
            .Where(o => o.Id == guid)
            .Select(o => o.Name)
            .FirstOrDefault();
    }

    private void ResolveOrganizations(PwEngineContext ctx, List<PersonaId> personaIds, Dictionary<PersonaId, Persona> result)
    {
        var guids = personaIds
            .Select(p => Guid.TryParse(p.Value, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        if (guids.Count == 0)
            return;

        var orgs = ctx.Organizations
            .Where(o => guids.Contains(o.Id))
            .ToList();

        foreach (var org in orgs)
        {
            var personaId = PersonaId.FromOrganization(OrganizationId.From(org.Id));
            var persona = OrganizationPersona.Create(
                OrganizationId.From(org.Id),
                org.Name
            );
            result[personaId] = persona;
        }
    }

    #endregion

    #region Coinhouse Resolution

    private CoinhousePersona? ResolveCoinhouse(PwEngineContext ctx, PersonaId personaId)
    {
        var tag = personaId.Value;
        var coinhouse = ctx.CoinHouses.FirstOrDefault(c => c.Tag == tag);
        if (coinhouse == null)
            return null;

        return CoinhousePersona.Create(
            new CoinhouseTag(coinhouse.Tag),
            SettlementId.Parse(coinhouse.Settlement),
            $"Coinhouse: {coinhouse.Tag}"
        );
    }

    private bool ExistsCoinhouse(PwEngineContext ctx, PersonaId personaId)
    {
        var tag = personaId.Value;
        return ctx.CoinHouses.Any(c => c.Tag == tag);
    }

    private string? GetCoinhouseDisplayName(PwEngineContext ctx, PersonaId personaId)
    {
        var tag = personaId.Value;
        return ctx.CoinHouses
            .Where(c => c.Tag == tag)
            .Select(c => "Coinhouse: " + c.Tag)
            .FirstOrDefault();
    }

    private void ResolveCoinhouses(PwEngineContext ctx, List<PersonaId> personaIds, Dictionary<PersonaId, Persona> result)
    {
        var tags = personaIds.Select(p => p.Value).ToList();
        if (tags.Count == 0)
            return;

        var coinhouses = ctx.CoinHouses
            .Where(c => tags.Contains(c.Tag))
            .ToList();

        foreach (var coinhouse in coinhouses)
        {
            var personaId = PersonaId.FromCoinhouse(new CoinhouseTag(coinhouse.Tag));
            var persona = CoinhousePersona.Create(
                new CoinhouseTag(coinhouse.Tag),
                SettlementId.Parse(coinhouse.Settlement),
                $"Coinhouse: {coinhouse.Tag}"
            );
            result[personaId] = persona;
        }
    }

    #endregion

    #region SystemProcess Resolution

    private SystemPersona ResolveSystemProcess(PersonaId personaId)
    {
        return SystemPersona.Create(personaId.Value);
    }

    private void ResolveSystemProcesses(List<PersonaId> personaIds, Dictionary<PersonaId, Persona> result)
    {
        foreach (var personaId in personaIds)
        {
            var persona = SystemPersona.Create(personaId.Value);
            result[personaId] = persona;
        }
    }

    #endregion
}

