﻿using AmiaReforged.Core.Entities;
using AmiaReforged.Core.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AmiaReforged.Core;

public class AmiaContext : DbContext
{
    private readonly string _connectionString;

    public AmiaContext()
    {
        _connectionString = ConnectionString();
    }

    public AmiaContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public AmiaContext(DbContextOptions<AmiaContext> options, string connectionString)
        : base(options)
    {
        _connectionString = connectionString;
    }

    public virtual DbSet<Ban> Bans { get; set; } = null!;
    public virtual DbSet<Dm> Dms { get; set; } = null!;
    public virtual DbSet<DmLogin> DmLogins { get; set; } = null!;
    public virtual DbSet<DreamcoinRecord> DreamcoinRecords { get; set; } = null!;
    public virtual DbSet<Player> Players { get; set; } = null!;
    public virtual DbSet<AmiaCharacter> Characters { get; set; } = null!;
    public DbSet<Faction> Factions { get; set; } = null!;
    public DbSet<FactionRelation> FactionRelations { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(_connectionString);
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
    
        modelBuilder.Entity<Dm>(entity =>
        {
            entity.HasNoKey();
    
            entity.ToTable("dms");
    
            entity.HasIndex(e => e.CdKey, "dms_cd_key_key")
                .IsUnique();
    
            entity.Property(e => e.CdKey).HasColumnName("cd_key");
        });
    
        modelBuilder.Entity<DmLogin>(entity =>
        {
            entity.HasKey(e => e.LoginNumber)
                .HasName("dm_logins_pkey");
    
            entity.ToTable("dm_logins");
    
            entity.Property(e => e.LoginNumber).HasColumnName("login_number");
    
            entity.Property(e => e.CdKey)
                .HasMaxLength(10)
                .HasColumnName("cd_key");
    
            entity.Property(e => e.LoginName)
                .HasMaxLength(255)
                .HasColumnName("login_name");
    
            entity.Property(e => e.SessionEnd)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("session_end");
    
            entity.Property(e => e.SessionStart)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("session_start");
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
    //
    //
    //     modelBuilder.Entity<AmiaCharacter>(entity =>
    //     {
    //         entity.HasKey(e => e.Id)
    //             .HasName("character_id");
    //
    //         entity.ToTable("characters");
    //
    //
    //         entity.Property(e => e.Id)
    //             .HasColumnName("id");
    //
    //         entity.Property(e => e.FirstName)
    //             .HasColumnName("first_name");
    //
    //         entity.Property(e => e.LastName)
    //             .HasColumnName("last_name");
    //
    //         entity.Property(e => e.CdKey)
    //             .HasColumnName("cd_key");
    //
    //         entity.Property(e => e.IsPlayerCharacter)
    //             .HasColumnName("is_player_character");
    //     });
    //
    //     modelBuilder.Entity<Faction>(entity =>
    //     {
    //         entity.HasKey(e => e.Name)
    //             .HasName("faction_name");
    //
    //         entity.ToTable("factions");
    //
    //         entity.Property(e => e.Name)
    //             .HasColumnName("name");
    //
    //         entity.Property(e => e.Description)
    //             .HasColumnName("description");
    //
    //         entity.Property(e => e.Members).HasColumnName("members");
    //     });
    }
}