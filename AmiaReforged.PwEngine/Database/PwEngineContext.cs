using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AmiaReforged.PwEngine.Database;

public class PwEngineContext : DbContext
{
    private readonly string _connectionString = ConnectionString();
    public DbSet<WorldCharacter> Characters { get; set; } = null!;
    public DbSet<ItemStorage> StorageContainers { get; set; } = null!;
    public DbSet<JobItem> Items { get; set; } = null!;
    public DbSet<StoredJobItem> StoredJobItems { get; set; } = null!;
    public DbSet<ItemStorageUser> ItemStorageUsers { get; set; } = null!;

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
}