using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class GroupMutationOverrideConfiguration : IEntityTypeConfiguration<GroupMutationOverride>
{
    public void Configure(EntityTypeBuilder<GroupMutationOverride> builder)
    {
        builder.ToTable("GroupMutationOverrides");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.SpawnGroupId)
            .HasColumnName("spawn_group_id")
            .IsRequired();

        builder.Property(o => o.MutationTemplateId)
            .HasColumnName("mutation_template_id")
            .IsRequired();

        builder.Property(o => o.ChancePercent)
            .HasColumnName("chance_percent")
            .HasDefaultValue(10);

        // Unique composite — one override per (group, mutation) pair
        builder.HasIndex(o => new { o.SpawnGroupId, o.MutationTemplateId })
            .IsUnique()
            .HasDatabaseName("IX_GroupMutationOverrides_Group_Mutation");

        builder.HasIndex(o => o.SpawnGroupId)
            .HasDatabaseName("IX_GroupMutationOverrides_SpawnGroupId");

        builder.HasIndex(o => o.MutationTemplateId)
            .HasDatabaseName("IX_GroupMutationOverrides_MutationTemplateId");
    }
}
