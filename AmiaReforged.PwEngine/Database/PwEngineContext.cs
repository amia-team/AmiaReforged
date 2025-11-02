using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Database.Entities.Economy.Properties;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
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

    public DbSet<StallTransaction> StallTransactions { get; set; } = null!;

    public DbSet<NpcShopRecord> NpcShops { get; set; } = null!;
    public DbSet<NpcShopProductRecord> NpcShopProducts { get; set; } = null!;
    public DbSet<NpcShopLedgerEntry> NpcShopLedgerEntries { get; set; } = null!;
    public DbSet<NpcShopVaultItem> NpcShopVaultItems { get; set; } = null!;

    public DbSet<CoinHouse> CoinHouses { get; set; } = null!;

    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<OrganizationMemberRecord> OrganizationMembers { get; set; } = null!;

    /// <summary>
    /// Generic catch all for any physical item that must be kept track of in storage.
    /// </summary>
    public DbSet<Storage> Warehouses { get; set; } = null!;

    /// <summary>
    ///  All items stored within warehouses.
    /// </summary>
    public DbSet<StoredItem> WarehouseItems { get; set; } = null!;

    public DbSet<CoinHouseAccount> CoinHouseAccounts { get; set; } = null!;
    public DbSet<CoinHouseAccountHolder> CoinHouseAccountHolders { get; set; } = null!;
    public DbSet<CoinHouseTransaction> CoinHouseTransactions { get; set; } = null!;

    /// <summary>
    /// Persona-based transaction log for gold transfers between any persona types.
    /// Supports Character, Organization, Coinhouse, Government, System transfers.
    /// </summary>
    public DbSet<Transaction> Transactions { get; set; } = null!;

    public DbSet<RentablePropertyRecord> RentableProperties { get; set; } = null!;
    public DbSet<RentablePropertyResidentRecord> RentablePropertyResidents { get; set; } = null!;


    public PwEngineContext()
    {
        _connectionString = ConnectionString();
    }

    public PwEngineContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Constructor for testing with DbContextOptions (e.g., InMemory database).
    /// </summary>
    public PwEngineContext(DbContextOptions<PwEngineContext> options) : base(options)
    {
        _connectionString = string.Empty; // Not used when options are provided
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
        modelBuilder.ApplyConfiguration(new CoinHouseConfiguration());
        modelBuilder.ApplyConfiguration(new CoinHouseAccountConfiguration());
        modelBuilder.ApplyConfiguration(new CoinHouseAccountHolderConfiguration());
        modelBuilder.ApplyConfiguration(new CoinHouseTransactionConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
        modelBuilder.ApplyConfiguration(new OrganizationMemberConfiguration());
        modelBuilder.ApplyConfiguration(new TransactionConfiguration());
        modelBuilder.ApplyConfiguration(new RentablePropertyConfiguration());
        modelBuilder.ApplyConfiguration(new RentablePropertyResidentConfiguration());
        modelBuilder.ApplyConfiguration(new NpcShopConfiguration());
        modelBuilder.ApplyConfiguration(new NpcShopProductConfiguration());
        modelBuilder.ApplyConfiguration(new NpcShopLedgerEntryConfiguration());
        modelBuilder.ApplyConfiguration(new NpcShopVaultItemConfiguration());
    }
}
