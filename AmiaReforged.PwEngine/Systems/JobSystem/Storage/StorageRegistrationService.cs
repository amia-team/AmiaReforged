using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.JobSystem.Storage;

[ServiceBinding(typeof(StorageRegistrationService))]
public class StorageRegistrationService
{
    // private readonly IRepository<JobItem, long> _itemRepository;
    // private readonly IRepository<ItemStorage, long> _storageRepository;
    // private readonly IRepository<StoredJobItem, long> _storedItemRepository;

    public StorageRegistrationService()
    {
        // _storedItemRepository =
        //     RepositoryBuilder.Create().WithContext(jobSystemContext).Build<StoredJobItem, long>();
        // _itemRepository = RepositoryBuilder.Create().WithContext(jobSystemContext).Build<JobItem, long>();
        // _storageRepository = RepositoryBuilder.Create().WithContext(jobSystemContext).Build<ItemStorage, long>();

        IEnumerable<NwPlaceable> storageObjects =
            NwObject.FindObjectsOfType<NwPlaceable>().Where(p => p.Tag == "pw_job_storage").ToArray();

        foreach (NwPlaceable storageObject in storageObjects)
        {
            storageObject.OnUsed += OpenStorage;
        }
    }

    private void OpenStorage(PlaceableEvents.OnUsed obj)
    {
        // TODO: Open storage window.
    }
}