using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngineTest.Integration;

public static class SeedData
{
    public static void SeedDatabase()
    {
        using PwEngineContext context = new();
        context.Characters.AddRange(new WorldCharacter
            {
                Id = 1,
                FirstName = "Test",
                LastName = "Character"
            },
            new WorldCharacter
            {
                Id = 2,
                FirstName = "Test",
                LastName = "Character2"
            }
        );
        context.Items.AddRange(new JobItem
        {
            Id = 1,
            Name = "Sword",
            Description = "Test Sword",
            BaseValue = 1,
            Material = MaterialEnum.AshWood,
            Quality = QualityEnum.Average,
            Type = ItemType.Weapon,
            WorldCharacterId = 1,
            ResRef = "ds_fake"
        });
        context.StorageContainers.AddRange(new ItemStorage
        {
            Id = 1
        });
        context.StoredJobItems.AddRange(new StoredJobItem
        {
            Id = 1,
            JobItemId = 1,
            ItemStorageId = 1
        });
        context.ItemStorageUsers.AddRange(new ItemStorageUser
        {
            Id = 1,
            ItemStorageId = 1,
            WorldCharacterId = 1
        });
        
        context.SaveChanges();
    }
}