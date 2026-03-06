using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

public class TraitGlyphBindingConfiguration : IEntityTypeConfiguration<TraitGlyphBinding>
{
    public void Configure(EntityTypeBuilder<TraitGlyphBinding> builder)
    {
        builder.ToTable("TraitGlyphBindings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.TraitTag)
            .HasColumnName("trait_tag")
            .HasMaxLength(128)
            .IsRequired();

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

        builder.HasIndex(b => new { b.TraitTag, b.GlyphDefinitionId })
            .IsUnique();
    }
}
