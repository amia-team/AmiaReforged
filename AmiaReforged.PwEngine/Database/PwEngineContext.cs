using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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

public class CharacterStatisticsConfiguration : IEntityTypeConfiguration<CharacterStatistics>
{
    public void Configure(EntityTypeBuilder<CharacterStatistics> builder)
    {
        builder.ToTable("CharacterStatistics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Required properties
        builder.Property(x => x.CharacterId).IsRequired();
        builder.Property(x => x.KnowledgePoints).IsRequired();
        builder.Property(x => x.TimesDied).IsRequired();
        builder.Property(x => x.TimesRankedUp).IsRequired();
        builder.Property(x => x.IndustriesJoined).IsRequired();
        builder.Property(x => x.PlayTime).IsRequired();

        // Index to ensure at most one stats row per character
        // Remove .IsUnique() if you intend to allow multiple rows per character.
        builder.HasIndex(x => x.CharacterId).IsUnique();

        builder.HasOne<PersistedCharacter>()
               .WithOne(c => c.Statistics)
               .HasForeignKey<CharacterStatistics>(x => x.CharacterId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

