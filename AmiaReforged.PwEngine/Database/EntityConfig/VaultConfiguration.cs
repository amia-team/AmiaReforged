using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AmiaReforged.PwEngine.Database.EntityConfig;

public class VaultConfiguration : IEntityTypeConfiguration<Vault>
{
    public void Configure(EntityTypeBuilder<Vault> builder)
    {
        builder.ToTable("Vaults");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.OwnerCharacterId)
            .IsRequired();

        builder.Property(v => v.AreaResRef)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(v => v.Balance)
            .IsRequired();

        builder.Property(v => v.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(v => v.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(v => new { v.OwnerCharacterId, v.AreaResRef })
            .IsUnique();
    }
}

