using AmiaReforged.Core.Models.Sailing;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>
/// Sailing-only persistence model. It preserves the shared context's legacy
/// ship mappings without adding sailing schema requirements to other services.
/// Schema creation or migration is never performed by this context at runtime.
/// </summary>
public sealed class SailingPersistenceDbContext(DbContextOptions<SailingPersistenceDbContext> options)
    : DbContext(options)
{
    public DbSet<SavedShipState> SavedShipStates => Set<SavedShipState>();
    public DbSet<SavedChartDiscovery> ChartDiscovery => Set<SavedChartDiscovery>();

    public DbSet<SavedMaritimeEconomy> MaritimeEconomy => Set<SavedMaritimeEconomy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SavedMaritimeEconomy>(entity =>
        {
            entity.ToTable("saved_maritime_economy");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Payload).HasColumnName("payload").HasColumnType("text").IsRequired();
        });
        modelBuilder.Entity<SavedShipState>(entity =>
        {
            entity.ToTable("saved_ship_states");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ShipName).HasColumnName("ship_name").IsRequired();
            entity.HasIndex(e => e.ShipName).IsUnique();
            entity.Property(e => e.AreaResRef).HasColumnName("area_res_ref").IsRequired();
            entity.Property(e => e.X).HasColumnName("x");
            entity.Property(e => e.Y).HasColumnName("y");
            entity.Property(e => e.Z).HasColumnName("z");
            entity.Property(e => e.Heading).HasColumnName("heading");
            entity.Property(e => e.Underway).HasColumnName("underway");
            entity.Property(e => e.Hull).HasColumnName("hull");
            entity.Property(e => e.WeaponResRef).HasColumnName("weapon_res_ref").IsRequired();
            // Explicit mappings override NotMapped on the shared entity.
            entity.Property(e => e.ShipId).HasColumnName("ship_id");
            entity.Property(e => e.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("text");
            entity.HasIndex(e => e.ShipId).IsUnique();
        });

        modelBuilder.Entity<SavedChartDiscovery>(entity =>
        {
            entity.ToTable("saved_chart_discovery");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccountKey).HasColumnName("account_key").IsRequired();
            entity.Property(e => e.AreaResRef).HasColumnName("area_res_ref").IsRequired();
            entity.Property(e => e.Cells).HasColumnName("cells").HasMaxLength(256).IsRequired();
            entity.HasIndex(e => new { e.AccountKey, e.AreaResRef }).IsUnique();
        });
    }
}
