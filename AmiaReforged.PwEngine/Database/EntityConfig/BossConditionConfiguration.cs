using AmiaReforged.PwEngine.Features.Encounters.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class BossConditionConfiguration : IEntityTypeConfiguration<BossCondition>
{
    public void Configure(EntityTypeBuilder<BossCondition> builder)
    {
        builder.ToTable("BossConditions");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.BossConfigId)
            .HasColumnName("boss_config_id")
            .IsRequired();

        builder.Property(c => c.Type)
            .HasColumnName("type")
            .HasConversion<int>();

        builder.Property(c => c.Operator)
            .HasColumnName("operator")
            .HasMaxLength(16);

        builder.Property(c => c.Value)
            .HasColumnName("value")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(c => c.BossConfigId)
            .HasDatabaseName("IX_BossConditions_BossConfigId");
    }
}
