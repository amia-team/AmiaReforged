using AmiaReforged.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Accessors;

public class CharacterAccessor : DbContext
{
    public DbSet<AmiaCharacter> Characters { get; set; }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql("Host=localhost;Database=amia;Username=amia;Password=amia");
    }
}