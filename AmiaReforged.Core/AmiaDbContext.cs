﻿using AmiaReforged.Core.Models;
using AmiaReforged.Core.Models.DmModels;
using AmiaReforged.Core.Models.Faction;
using AmiaReforged.Core.Models.World;
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
    public DbSet<SavedQuickslots> SavedQuickslots { get; set; } = null!;
    public DbSet<InvasionRecord> InvasionRecord { get; set; } = null!;
    public DbSet<LastLocation> LastLocation { get; set; } = null!;
    public DbSet<PersistPLC> PersistPLC { get; set; } = null!;
    public DbSet<Encounter> Encounters { get; set; } = null!;
    public DbSet<EncounterEntry> EncounterEntries { get; set; } = null!;
    public DbSet<Npc> Npcs { get; set; } = null!;
    public DbSet<WorldConfiguration> WorldEngineConfig { get; set; }

    public DbSet<DmArea> DmAreas { get; set; } = null!;
    public DbSet<PlayerPlaytimeRecord> PlayerPlaytimeRecords { get; set; } = null!;
    public DbSet<DmPlaytimeRecord> DmPlaytimeRecords { get; set; } = null!;


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

        modelBuilder.Entity<PlayerPlaytimeRecord>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("player_playtime_records_pkey");

            entity.ToTable("player_playtime_records");

            entity.HasIndex(e => new { e.CdKey, e.WeekStart }, "player_playtime_records_cdkey_weekstart_key")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CdKey)
                .HasColumnType("character varying")
                .HasColumnName("cd_key");
            entity.Property(e => e.WeekStart).HasColumnName("week_start");
            entity.Property(e => e.MinutesPlayed).HasColumnName("minutes_played");
            entity.Property(e => e.MinutesTowardNextDc).HasColumnName("minutes_toward_next_dc");
            entity.Property(e => e.LastUpdated).HasColumnName("last_updated");

            entity.HasOne(d => d.Player)
                .WithMany(p => p.PlaytimeRecords)
                .HasForeignKey(d => d.CdKey)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("player_playtime_records_cd_key_fkey");
        });

        modelBuilder.Entity<DmPlaytimeRecord>(entity =>
        {
            entity.HasKey(e => e.Id)
                .HasName("dm_playtime_records_pkey");

            entity.ToTable("dm_playtime_records");

            entity.HasIndex(e => new { e.CdKey, e.WeekStart }, "dm_playtime_records_cdkey_weekstart_key")
                .IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CdKey)
                .HasColumnType("character varying")
                .HasColumnName("cd_key");
            entity.Property(e => e.WeekStart).HasColumnName("week_start");
            entity.Property(e => e.MinutesPlayed).HasColumnName("minutes_played");
            entity.Property(e => e.MinutesTowardNextDc).HasColumnName("minutes_toward_next_dc");
            entity.Property(e => e.LastUpdated).HasColumnName("last_updated");

            entity.HasOne(d => d.Dm)
                .WithMany(p => p.PlaytimeRecords)
                .HasForeignKey(d => d.CdKey)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("dm_playtime_records_cd_key_fkey");
        });
    }
}
