using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

using OrganizationEntity = AmiaReforged.PwEngine.Database.Entities.Organization;
using DomainOrganization = AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Organization;

namespace AmiaReforged.PwEngine.Database;

/// <summary>
/// PostgreSQL-backed repository for <see cref="IOrganization"/> aggregate roots.
/// </summary>
[ServiceBinding(typeof(IOrganizationRepository))]
public class PersistentOrganizationRepository(PwContextFactory factory) : IOrganizationRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public void Add(IOrganization organization)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            OrganizationEntity entity = MapToEntity(organization);
            ctx.Organizations.Add(entity);
            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist organization {Organization}", organization.Id);
            throw;
        }
    }

    public IOrganization? GetById(OrganizationId id)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();

            OrganizationEntity? record = ctx.Organizations
                .AsNoTracking()
                .FirstOrDefault(o => o.Id == id.Value);

            return record is null ? null : MapToDomain(record);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load organization {Organization}", id);
            return null;
        }
    }

    public List<IOrganization> GetAll()
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();

            List<OrganizationEntity> records = ctx.Organizations
                .AsNoTracking()
                .ToList();

            return records.Select(MapToDomain).Cast<IOrganization>().ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load organizations");
            return [];
        }
    }

    public List<IOrganization> GetByType(OrganizationType type)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();

            List<OrganizationEntity> records = ctx.Organizations
                .AsNoTracking()
                .Where(o => o.Type == type)
                .ToList();

            return records.Select(MapToDomain).Cast<IOrganization>().ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load organizations of type {Type}", type);
            return [];
        }
    }

    public void Update(IOrganization organization)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            OrganizationEntity? existing = ctx.Organizations.FirstOrDefault(o => o.Id == organization.Id.Value);

            if (existing is null)
            {
                ctx.Organizations.Add(MapToEntity(organization));
            }
            else
            {
                ApplyUpdates(existing, organization);
                ctx.Organizations.Update(existing);
            }

            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update organization {Organization}", organization.Id);
            throw;
        }
    }

    public void SaveChanges()
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        ctx.SaveChanges();
    }

    private static OrganizationEntity MapToEntity(IOrganization organization)
    {
        return new OrganizationEntity
        {
            Id = organization.Id.Value,
            Name = organization.Name,
            Description = organization.Description,
            Type = organization.Type,
            ParentOrganizationId = organization.ParentOrganization?.Value,
            BanListJson = SerializeBanList(organization.BanList),
            InboxJson = SerializeInbox(organization.GetInbox()),
            PersonaIdString = PersonaId.FromOrganization(organization.Id).ToString()
        };
    }

    private static void ApplyUpdates(OrganizationEntity entity, IOrganization organization)
    {
        entity.Name = organization.Name;
        entity.Description = organization.Description;
        entity.Type = organization.Type;
        entity.ParentOrganizationId = organization.ParentOrganization?.Value;
        entity.BanListJson = SerializeBanList(organization.BanList);
        entity.InboxJson = SerializeInbox(organization.GetInbox());
        entity.PersonaIdString = PersonaId.FromOrganization(organization.Id).ToString();
    }

    private static DomainOrganization MapToDomain(OrganizationEntity record)
    {
        OrganizationId id = OrganizationId.From(record.Id);
        OrganizationId? parent = record.ParentOrganizationId.HasValue
            ? OrganizationId.From(record.ParentOrganizationId.Value)
            : null;

        DomainOrganization domain = DomainOrganization.Create(
            id,
            record.Name,
            record.Description ?? string.Empty,
            record.Type,
            parent);

        domain.BanList.AddRange(DeserializeBanList(record.BanListJson));
        domain.Inbox.AddRange(DeserializeInbox(record.InboxJson));

        return domain;
    }

    private static string SerializeBanList(IReadOnlyCollection<CharacterId> bans)
    {
        if (bans.Count == 0)
        {
            return "[]";
        }

        List<Guid> values = bans.Select(static b => b.Value).ToList();
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static List<CharacterId> DeserializeBanList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            List<Guid>? values = JsonSerializer.Deserialize<List<Guid>>(json, JsonOptions);
            if (values is null)
            {
                return [];
            }

            List<CharacterId> result = new();
            foreach (Guid value in values)
            {
                if (value == Guid.Empty)
                {
                    continue;
                }

                try
                {
                    result.Add(CharacterId.From(value));
                }
                catch (ArgumentException)
                {
                    // Ignore invalid identifiers to avoid breaking deserialization
                }
            }

            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeInbox(IReadOnlyList<OrganizationRequest> requests)
    {
        if (requests.Count == 0)
        {
            return "[]";
        }

        List<OrganizationRequestDto> dto = requests
            .Select(static request => new OrganizationRequestDto
            {
                CharacterId = request.CharacterId,
                OrganizationId = request.OrganizationId.Value,
                Action = request.Action,
                Message = request.Message
            })
            .ToList();

        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    private static List<OrganizationRequest> DeserializeInbox(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            List<OrganizationRequestDto>? dto = JsonSerializer.Deserialize<List<OrganizationRequestDto>>(json, JsonOptions);
            if (dto is null)
            {
                return [];
            }

            List<OrganizationRequest> requests = new();

            foreach (OrganizationRequestDto entry in dto)
            {
                if (entry.OrganizationId == Guid.Empty)
                {
                    continue;
                }

                OrganizationId orgId;
                try
                {
                    orgId = OrganizationId.From(entry.OrganizationId);
                }
                catch (ArgumentException)
                {
                    continue;
                }

                requests.Add(new OrganizationRequest(
                    entry.CharacterId,
                    orgId,
                    entry.Action,
                    entry.Message));
            }

            return requests;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private sealed class OrganizationRequestDto
    {
        public Guid CharacterId { get; init; }
        public Guid OrganizationId { get; init; }
        public OrganizationActionType Action { get; init; }
        public string? Message { get; init; }
    }
}
