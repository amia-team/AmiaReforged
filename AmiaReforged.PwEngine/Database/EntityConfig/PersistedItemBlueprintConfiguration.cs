using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class PersistedItemBlueprintConfiguration : IEntityTypeConfiguration<PersistedItemBlueprint>
{
    public void Configure(EntityTypeBuilder<PersistedItemBlueprint> builder)
    {
        builder.ToTable("ItemBlueprints");

        builder.HasKey(e => e.ItemTag);

        builder.Property(e => e.ResRef)
            .HasColumnName("res_ref")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(e => e.ItemTag)
            .HasColumnName("item_tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(e => e.BaseItemType)
            .HasColumnName("base_item_type");

        builder.Property(e => e.BaseValue)
            .HasColumnName("base_value")
            .HasDefaultValue(1);

        builder.Property(e => e.WeightIncreaseConstant)
            .HasColumnName("weight_increase_constant")
            .HasDefaultValue(-1);

        builder.Property(e => e.JobSystemType)
            .HasColumnName("job_system_type")
            .HasMaxLength(64)
            .HasDefaultValue("None");

        builder.Property(e => e.MaterialsJson)
            .HasColumnName("materials")
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(e => e.AppearanceJson)
            .HasColumnName("appearance")
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(e => e.LocalVariablesJson)
            .HasColumnName("local_variables")
            .HasColumnType("jsonb");

        builder.Property(e => e.SourceFile)
            .HasColumnName("source_file")
            .HasMaxLength(256);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(e => e.ResRef)
            .HasDatabaseName("IX_ItemBlueprints_ResRef");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("IX_ItemBlueprints_Name");

        builder.HasIndex(e => e.JobSystemType)
            .HasDatabaseName("IX_ItemBlueprints_JobSystemType");
    }
}
