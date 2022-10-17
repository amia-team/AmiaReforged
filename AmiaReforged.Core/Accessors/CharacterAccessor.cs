using AmiaReforged.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.Core.Accessors;

public class CharacterAccessor : DbContext
{
    public DbSet<AmiaCharacter> Characters { get; set; }
}