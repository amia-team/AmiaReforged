using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;
using AmiaReforged.PwEngine.Systems.WorldEngine.Models;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(PwEngineContext))]
public class PwEngineContext : DbContext
{
    private readonly string _connectionString;
    public DbSet<Character> Characters { get; set; } = null!;
    public DbSet<PersistedWorldCharacter> WorldCharacters { get; set; } = null!;
    public DbSet<ItemStorage> StorageContainers { get; set; } = null!;
    public DbSet<JobItem> Items { get; set; } = null!;
    public DbSet<StoredJobItem> StoredJobItems { get; set; } = null!;
    public DbSet<ItemStorageUser> ItemStorageUsers { get; set; } = null!;

    public DbSet<WorldConfiguration> WorldConfiguration { get; set; } = null!;


    public PwEngineContext()
    {
        _connectionString = ConnectionString();
    }

    public PwEngineContext(string connectionString)
    {
        _connectionString = connectionString;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseNpgsql(_connectionString);
            optionsBuilder.EnableSensitiveDataLogging();
        }
    }

    private static string ConnectionString()
    {
        NpgsqlConnectionStringBuilder connectionBuilder = new()
        {
            Database = EngineDbConfig.Database,
            Host = EngineDbConfig.Host,
            Username = EngineDbConfig.Username,
            Password = EngineDbConfig.Password,
            Port = EngineDbConfig.Port
        };
        return connectionBuilder.ConnectionString;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        EntityTypeBuilder<Character> b = modelBuilder.Entity<Character>();
        b.HasKey(c => c.Id);
        b.Property(c => c.Name).IsRequired().HasMaxLength(128);
        b.Property(c => c.IsActive).IsRequired();

        // Persist Character.Owner as a single string column using a converter + comparer
        b.Property(c => c.Owner)
            .HasColumnName("owner")
            .HasMaxLength(256)
            .IsRequired()
            .HasConversion(new CharacterOwnerConverter())
            .Metadata.SetValueComparer(new CharacterOwnerComparer());

        // Optional index if you frequently filter by Owner
        b.HasIndex(c => c.Owner).HasDatabaseName("ix_characters_owner");
    }
}
