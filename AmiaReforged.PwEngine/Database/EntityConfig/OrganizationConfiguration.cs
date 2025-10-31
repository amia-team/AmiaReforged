using AmiaReforged.PwEngine.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.Property(o => o.Name)
            .IsRequired();

        builder.Property(o => o.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        builder.Property(o => o.ParentOrganizationId)
            .HasColumnName("parent_organization_id");

        builder.Property(o => o.BanListJson)
            .HasColumnName("ban_list")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(o => o.InboxJson)
            .HasColumnName("inbox")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(o => o.PersonaIdString)
            .HasColumnName("persona_id");

        builder.HasIndex(o => o.Type)
            .HasDatabaseName("IX_Organizations_Type");

        builder.HasIndex(o => o.ParentOrganizationId)
            .HasDatabaseName("IX_Organizations_ParentOrganizationId");
    }
}
