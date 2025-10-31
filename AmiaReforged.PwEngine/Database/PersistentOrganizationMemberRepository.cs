using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Database;

/// <summary>
///     PostgreSQL-backed implementation of <see cref="IOrganizationMemberRepository"/>.
/// </summary>
[ServiceBinding(typeof(IOrganizationMemberRepository))]
public class PersistentOrganizationMemberRepository(PwContextFactory factory) : IOrganizationMemberRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public void Add(OrganizationMember member)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        OrganizationMemberRecord entity = MapToEntity(member);
        ctx.OrganizationMembers.Add(entity);
        ctx.SaveChanges();
    }

    public OrganizationMember? GetByCharacterAndOrganization(CharacterId characterId, OrganizationId organizationId)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();

            OrganizationMemberRecord? record = ctx.OrganizationMembers.AsNoTracking()
                .FirstOrDefault(m => m.CharacterId == characterId.Value && m.OrganizationId == organizationId.Value);

            return record is null ? null : MapToDomain(record);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load organization member {Character} in {Organization}", characterId, organizationId);
            return null;
        }
    }

    public List<OrganizationMember> GetByOrganization(OrganizationId organizationId)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();

            List<OrganizationMemberRecord> records = ctx.OrganizationMembers.AsNoTracking()
                .Where(m => m.OrganizationId == organizationId.Value)
                .ToList();

            return records.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load organization members for organization {Organization}", organizationId);
            return [];
        }
    }

    public List<OrganizationMember> GetByCharacter(CharacterId characterId)
    {
        try
        {
            using PwEngineContext ctx = factory.CreateDbContext();

            List<OrganizationMemberRecord> records = ctx.OrganizationMembers.AsNoTracking()
                .Where(m => m.CharacterId == characterId.Value)
                .ToList();

            return records.Select(MapToDomain).ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load organization memberships for character {Character}", characterId);
            return [];
        }
    }

    public void Update(OrganizationMember member)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        OrganizationMemberRecord? existing = ctx.OrganizationMembers
            .FirstOrDefault(m => m.Id == member.Id);

        if (existing is null)
        {
            ctx.OrganizationMembers.Add(MapToEntity(member));
        }
        else
        {
            ApplyUpdates(existing, member);
            ctx.OrganizationMembers.Update(existing);
        }

        ctx.SaveChanges();
    }

    public void Remove(OrganizationMember member)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        OrganizationMemberRecord? existing = ctx.OrganizationMembers
            .FirstOrDefault(m => m.Id == member.Id);

        if (existing is null)
        {
            return;
        }

        ctx.OrganizationMembers.Remove(existing);
        ctx.SaveChanges();
    }

    public void SaveChanges()
    {
        using PwEngineContext ctx = factory.CreateDbContext();
        ctx.SaveChanges();
    }

    private static OrganizationMemberRecord MapToEntity(OrganizationMember member)
    {
        return new OrganizationMemberRecord
        {
            Id = member.Id == Guid.Empty ? Guid.NewGuid() : member.Id,
            CharacterId = member.CharacterId.Value,
            OrganizationId = member.OrganizationId.Value,
            Rank = member.Rank,
            Status = member.Status,
            JoinedDate = member.JoinedDate,
            DepartedDate = member.DepartedDate,
            Notes = member.Notes,
            RolesJson = SerializeRoles(member.Roles),
            MetadataJson = SerializeMetadata(member.Metadata)
        };
    }

    private static void ApplyUpdates(OrganizationMemberRecord record, OrganizationMember member)
    {
        record.CharacterId = member.CharacterId.Value;
        record.OrganizationId = member.OrganizationId.Value;
        record.Rank = member.Rank;
        record.Status = member.Status;
        record.JoinedDate = member.JoinedDate;
        record.DepartedDate = member.DepartedDate;
        record.Notes = member.Notes;
        record.RolesJson = SerializeRoles(member.Roles);
        record.MetadataJson = SerializeMetadata(member.Metadata);
    }

    private static OrganizationMember MapToDomain(OrganizationMemberRecord record)
    {
        List<MemberRole> roles = DeserializeRoles(record.RolesJson);
        Dictionary<string, object?> metadata = DeserializeMetadata(record.MetadataJson);

        return new OrganizationMember
        {
            Id = record.Id,
            CharacterId = new CharacterId(record.CharacterId),
            OrganizationId = OrganizationId.From(record.OrganizationId),
            Rank = record.Rank,
            Status = record.Status,
            JoinedDate = record.JoinedDate,
            DepartedDate = record.DepartedDate,
            Notes = record.Notes,
            Roles = roles,
            Metadata = metadata
        };
    }

    private static string SerializeRoles(IEnumerable<MemberRole> roles)
    {
        List<string> values = roles?.Select(r => r.Value).ToList() ?? [];
        return JsonSerializer.Serialize(values, JsonOptions);
    }

    private static List<MemberRole> DeserializeRoles(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            List<string>? roles = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            return roles?.Select(static value => new MemberRole(value)).ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string SerializeMetadata(Dictionary<string, object?>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return "{}";
        }

        return JsonSerializer.Serialize(metadata, JsonOptions);
    }

    private static Dictionary<string, object?> DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            Dictionary<string, JsonElement>? raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonOptions);
            if (raw is null)
            {
                return new Dictionary<string, object?>();
            }

            Dictionary<string, object?> result = new();
            foreach ((string key, JsonElement element) in raw)
            {
                result[key] = ConvertJsonElement(element);
            }

            return result;
        }
        catch (JsonException)
        {
            return new Dictionary<string, object?>();
        }
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out long l)
                ? l
                : element.TryGetDouble(out double d)
                    ? d
                    : element.GetDecimal(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Object => DeserializeMetadata(element.GetRawText()),
            JsonValueKind.Array => element
                .EnumerateArray()
                .Select(ConvertJsonElement)
                .ToList(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
    }
}
