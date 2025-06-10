using AmiaReforged.Core.Models;
using Anvil.Services;

namespace AmiaReforged.Core.Services;

[ServiceBinding(typeof(QuickslotLoader))]
public class QuickslotLoader
{
    private readonly IRepository<SavedQuickslots, long> _quickslotRepository;

    public QuickslotLoader()
    {
        _quickslotRepository = new Repository<SavedQuickslots, long>(new AmiaDbContext());
    }

    public async Task<IEnumerable<SavedQuickslots>> LoadQuickslots(Guid playerId) =>
        await _quickslotRepository.FindAll(q => q.PlayerCharacterId == playerId);

    public async Task SavePlayerQuickslots(string savedName, byte[] serializedQuickbar, Guid playerCharacterId)
    {
        SavedQuickslots quickslots = new()
        {
            PlayerCharacterId = playerCharacterId,
            Quickslots = serializedQuickbar,
            Name = savedName
        };

        SavedQuickslots? quickslotsWithSameName =
            await _quickslotRepository.Find(q =>
                q.Name == savedName && q.PlayerCharacterId == quickslots.PlayerCharacterId);

        if (quickslotsWithSameName != null)
        {
            quickslotsWithSameName.Quickslots = quickslots.Quickslots;
            await _quickslotRepository.Update(quickslotsWithSameName);
            return;
        }

        await _quickslotRepository.Add(quickslots);
    }

    public async Task<SavedQuickslots?> LoadSavedQuickslot(long id) =>
        await _quickslotRepository.Get(id);

    public async Task DeleteSavedQuickslot(long id)
    {
        await _quickslotRepository.Delete(id);
    }
}