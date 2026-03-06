using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

public class GlyphDefinitionConfiguration : IEntityTypeConfiguration<GlyphDefinition>
{
    public void Configure(EntityTypeBuilder<GlyphDefinition> builder)
    {
        builder.ToTable("GlyphDefinitions");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(g => g.Description)
            .HasColumnName("description")
            .HasMaxLength(512);

        builder.Property(g => g.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(g => g.GraphJson)
            .HasColumnName("graph_json")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(g => g.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(false);

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(g => g.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasMany(g => g.Bindings)
            .WithOne(b => b.GlyphDefinition)
            .HasForeignKey(b => b.GlyphDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
