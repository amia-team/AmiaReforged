using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Language.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

public sealed record GlyphModulePublication(GlyphModuleRevision Revision, DateTime PublishedAt);
public sealed class GlyphModule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string SourceText { get; set; } = "";
    public string PublishedVersionsJson { get; set; } = "[]";
    public Guid? ActiveRevisionId { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<GlyphModulePublication> History() => JsonSerializer.Deserialize<List<GlyphModulePublication>>(PublishedVersionsJson) ?? [];
}

public sealed class GlyphModuleConfiguration : IEntityTypeConfiguration<GlyphModule>
{
    public void Configure(EntityTypeBuilder<GlyphModule> b)
    {
        b.ToTable("GlyphModules"); b.HasKey(m => m.Id);
        b.Property(m => m.Name).HasMaxLength(128).IsRequired(); b.HasIndex(m => m.Name).IsUnique();
        b.Property(m => m.SourceText).HasColumnType("text").IsRequired();
        b.Property(m => m.PublishedVersionsJson).HasColumnType("text").HasDefaultValue("[]").IsRequired();
    }
}
