using AmiaReforged.Core.Models.Settlement;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Settlements.Tests.Integration;

internal class InMemoryDbContext : DbContext
{
    public InMemoryDbContext(DbContextOptions<InMemoryDbContext> options) : base(options)
    {
        
    }

    public DbSet<Material> Materials { get; set; }
    public DbSet<EconomyItem> BaseItems { get; set; }
    public DbSet<Quality> Qualities { get; set; }

}