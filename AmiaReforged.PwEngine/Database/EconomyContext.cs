using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(EconomyContext))]
public class EconomyContext : DbContext
{
    private readonly string _connectionString = ConnectionString();

    public DbSet<PersistentResourceNode> ExistingNodes { get; set; } = null!;
    public DbSet<SavedLocation> SavedLocations { get; set; } = null!;


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
            Database = PostgresConfig.Database,
            Host = PostgresConfig.Host,
            Username = PostgresConfig.Username,
            Password = PostgresConfig.Password,
            Port = PostgresConfig.Port
        };
        return connectionBuilder.ConnectionString;
    }
}
