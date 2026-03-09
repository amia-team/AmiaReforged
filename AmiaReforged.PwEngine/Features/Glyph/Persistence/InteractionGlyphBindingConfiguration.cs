using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

public class InteractionGlyphBindingConfiguration : IEntityTypeConfiguration<InteractionGlyphBinding>
{
    public void Configure(EntityTypeBuilder<InteractionGlyphBinding> builder)
    {
        builder.ToTable("InteractionGlyphBindings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.InteractionTag)
            .HasColumnName("interaction_tag")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(b => b.AreaResRef)
            .HasColumnName("area_resref")
            .HasMaxLength(32);

        builder.Property(b => b.GlyphDefinitionId)
            .HasColumnName("glyph_definition_id")
            .IsRequired();

        builder.Property(b => b.Priority)
            .HasColumnName("priority")
            .HasDefaultValue(0);

        builder.HasOne(b => b.GlyphDefinition)
            .WithMany()
            .HasForeignKey(b => b.GlyphDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: same tag + definition + area scope
        builder.HasIndex(b => new { b.InteractionTag, b.GlyphDefinitionId, b.AreaResRef })
            .IsUnique();

        // Performance index for tag lookups
        builder.HasIndex(b => b.InteractionTag);
    }
}
