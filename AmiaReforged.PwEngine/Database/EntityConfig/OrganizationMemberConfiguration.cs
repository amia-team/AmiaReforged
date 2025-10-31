using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// EF Core configuration for <see cref="OrganizationMemberRecord"/>.
/// </summary>
public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMemberRecord>
{
    public void Configure(EntityTypeBuilder<OrganizationMemberRecord> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.RolesJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(m => m.MetadataJson)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.HasIndex(m => new { m.CharacterId, m.OrganizationId })
            .IsUnique();

        builder.HasIndex(m => m.OrganizationId);
        builder.HasIndex(m => m.CharacterId);
    }
}
