using AmiaReforged.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NLog;

namespace AmiaReforged.Core.Accessors;

public class CharacterAccessor : DbContext
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public DbSet<AmiaCharacter> Characters { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string? connectionString = Environment.GetEnvironmentVariable("db_connection_string");
        if (connectionString == null)
        {
            Log.Error(
                "Could not find connection string in environment variables. Is db_connection_string set in the db.env file?");
            return;
        }

        optionsBuilder.UseNpgsql(connectionString);
    }
}