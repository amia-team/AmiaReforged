using AmiaReforged.PwEngine.Database;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Characters;

[ServiceBinding(typeof(ICharacterStatRepository))]
public class CharacterStatRepository(PwContextFactory factory) : ICharacterStatRepository
{
    private readonly PwEngineContext _ctx = factory.CreateDbContext();

    public CharacterStatistics? GetCharacterStatistics(Guid characterId)
    {
        return _ctx.CharacterStatistics.FirstOrDefault(x => x.CharacterId == characterId);
    }

    public void UpdateCharacterStatistics(CharacterStatistics statistics)
    {
        _ctx.Update(statistics);
    }

    public void SaveChanges()
    {
        _ctx.SaveChanges();
    }
}
