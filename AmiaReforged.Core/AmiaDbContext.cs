using AmiaReforged.Core.Models;
using AmiaReforged.Core.Models.Faction;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AmiaReforged.Core;

public class AmiaDbContext : DbContext
{
    private readonly string _connectionString = ConnectionString();

    public virtual DbSet<Ban> Bans { get; set; } = null!;
    public virtual DbSet<Dm> Dms { get; set; } = null!;
    public virtual DbSet<DmLogin> DmLogins { get; set; } = null!;
    public virtual DbSet<DreamcoinRecord> DreamcoinRecords { get; set; } = null!;
    public virtual DbSet<Player> Players { get; set; } = null!;
    public virtual DbSet<PlayerCharacter> Characters { get; set; } = null!;
    public DbSet<FactionEntity> Factions { get; set; } = null!;
    public DbSet<FactionRelation> FactionRelations { get; set; } = null!;
    public DbSet<FactionCharacterRelation> FactionCharacterRelations { get; set; } = null!;
    public DbSet<StoredItem> PlayerItems { get; set; } = null!;
    public DbSet<SavedSpellbook> SavedSpellbooks { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(_connectionString);
            optionsBuilder.EnableSensitiveDataLogging(true);
        }
    }

    private static string ConnectionString()
    {
        NpgsqlConnectionStringBuilder connectionBuilder = new()
        {
            Database = PostgresConfig.Database,
            Host = PostgresConfig.Host,
            Username = PostgresConfig.Username,
            Password = PostgresConfig.Password,
            Port = PostgresConfig.Port
        };
        return connectionBuilder.ConnectionString;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ban>(entity =>
        {
            entity.HasKey(e => e.CdKey);

            entity.ToTable("bans");

            entity.HasIndex(e => e.CdKey, "bans_cd_key_key")
                .IsUnique();

            entity.Property(e => e.CdKey).HasColumnName("cd_key");
        });

        modelBuilder.Entity<DreamcoinRecord>(entity =>
        {
            entity.HasKey(e => e.CdKey)
                .HasName("dreamcoin_records_pkey");

            entity.ToTable("dreamcoin_records");

            entity.Property(e => e.CdKey)
                .HasColumnType("character varying")
                .HasColumnName("cd_key");

            entity.Property(e => e.Amount).HasColumnName("amount");

            entity.HasOne(d => d.CdKeyNavigation)
                .WithOne(p => p.DreamcoinRecord)
                .HasForeignKey<DreamcoinRecord>(d => d.CdKey)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("dreamcoin_records_cd_key_fkey");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.CdKey)
                .HasName("players_pkey");

            entity.ToTable("players");

            entity.Property(e => e.CdKey)
                .HasColumnType("character varying")
                .HasColumnName("cd_key");
        });
    }
}