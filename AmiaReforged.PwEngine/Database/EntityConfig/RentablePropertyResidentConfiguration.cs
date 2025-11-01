using AmiaReforged.PwEngine.Database.Entities.Economy.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class RentablePropertyResidentConfiguration : IEntityTypeConfiguration<RentablePropertyResidentRecord>
{
    public void Configure(EntityTypeBuilder<RentablePropertyResidentRecord> builder)
    {
        builder.ToTable("rentable_property_residents");

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.PropertyId)
            .HasColumnName("property_id");

        builder.Property(r => r.Persona)
            .HasColumnName("persona")
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(r => new { r.PropertyId, r.Persona })
            .IsUnique()
            .HasDatabaseName("rentable_property_residents_property_persona_idx");
    }
}
