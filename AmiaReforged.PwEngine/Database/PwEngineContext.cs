using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
using AmiaReforged.PwEngine.Database.Entities.Shops;
using AmiaReforged.PwEngine.Database.EntityConfig;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.CharacterData;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(PwEngineContext))]
public class PwEngineContext : DbContext
{
    private readonly string _connectionString;
    public DbSet<PersistedCharacter> WorldCharacters { get; set; } = null!;

    public DbSet<WorldConfiguration> WorldConfiguration { get; set; } = null!;
    public DbSet<PersistentResourceNodeInstance> PersistedNodes { get; set; } = null!;

    public DbSet<PersistedCharacter> Characters { get; set; } = null!;
    public DbSet<CharacterStatistics> CharacterStatistics { get; set; } = null!;

    public DbSet<PersistentCharacterKnowledge> CharacterKnowledge { get; set; } = null!;

    public DbSet<PersistentIndustryMembership> IndustryMemberships { get; set; } = null!;

    public DbSet<PersistentCharacterTrait> CharacterTraits { get; set; } = null!;

    public DbSet<House> Houses { get; set; } = null!;

    public DbSet<PersistentObject> PersistentObjects { get; set; } = null!;

    public DbSet<PlayerStall> PlayerStalls { get; set; } = null!;
    public DbSet<StallProduct> StallProducts { get; set; } = null!;


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
        modelBuilder.ApplyConfiguration(new CharacterStatisticsConfiguration());
    }
}
