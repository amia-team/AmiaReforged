using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

public class SpawnProfileGlyphBindingConfiguration : IEntityTypeConfiguration<SpawnProfileGlyphBinding>
{
    public void Configure(EntityTypeBuilder<SpawnProfileGlyphBinding> builder)
    {
        builder.ToTable("SpawnProfileGlyphBindings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.SpawnProfileId)
            .HasColumnName("spawn_profile_id")
            .IsRequired();

        builder.Property(b => b.GlyphDefinitionId)
            .HasColumnName("glyph_definition_id")
            .IsRequired();

        builder.Property(b => b.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(0);

        builder.HasOne(b => b.SpawnProfile)
            .WithMany()
            .HasForeignKey(b => b.SpawnProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.GlyphDefinition)
            .WithMany(g => g.Bindings)
            .HasForeignKey(b => b.GlyphDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => new { b.SpawnProfileId, b.GlyphDefinitionId })
            .IsUnique()
            .HasDatabaseName("IX_SpawnProfileGlyphBindings_Profile_Definition");
    }
}
