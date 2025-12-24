using AmiaReforged.PwEngine.Database.Entities.Admin;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

/// <summary>
/// Configures persistent storage for rebuild item records.
/// </summary>
public sealed class RebuildItemRecordConfiguration : IEntityTypeConfiguration<RebuildItemRecord>
{
    public void Configure(EntityTypeBuilder<RebuildItemRecord> builder)
    {
        builder.ToTable("rebuild_item_records");

        builder.HasKey(r => r.Id)
            .HasName("rebuild_item_records_pkey");

        builder.Property(r => r.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(r => r.CharacterRebuildId)
            .HasColumnName("character_rebuild_id")
            .IsRequired();

        builder.Property(r => r.ItemData)
            .HasColumnName("item_data")
            .IsRequired();

        builder.HasOne(r => r.CharacterRebuild)
            .WithMany()
            .HasForeignKey(r => r.CharacterRebuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.CharacterRebuildId)
            .HasDatabaseName("rebuild_item_records_character_rebuild_id_idx");
    }
}

