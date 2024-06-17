using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.Services;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using Anvil.Services;

namespace AmiaReforged.Settlements.Services.Economy.Initialization;

[ServiceBinding(typeof(MaterialInitializer))]
public class MaterialInitializer : IResourceInitializer
{
    private readonly IResourceImporter<Material> _importer;
    private readonly NwTaskHelper _taskHelper;
    private readonly Repository<Material, int> _materials;

    public MaterialInitializer(IResourceImporter<Material> importer, IRepositoryFactory repositoryFactory,
        NwTaskHelper taskHelper)
    {
        _importer = importer;
        _taskHelper = taskHelper;
        _materials = ((Repository<Material, int>?)repositoryFactory.CreateRepository<Material, int>())!;
    }

    public async Task Initialize()
    {
        await ProcessMaterials();
    }

    private async Task ProcessMaterials()
    {
        foreach (Material material in _importer.LoadResources())
        {
            Material? dbMat = await FindMaterial(material.Name);
            if (dbMat != null)
            {
                UpdateMaterial(dbMat, material);
                await _materials.Update(dbMat);
            }
            else
            {
                await _materials.Add(material);
            }
        }
    }

    private async Task<Material?> FindMaterial(string materialName)
    {
        IEnumerable<Material?> materials = await _materials.GetAll();

        return materials.FirstOrDefault(m => m?.Name == materialName);
    }
    
    private static void UpdateMaterial(Material dbMat, Material material)
    {
        dbMat.Type = material.Type;
        dbMat.ValueModifier = material.ValueModifier;
        dbMat.MagicModifier = material.MagicModifier;
        dbMat.DurabilityModifier = material.DurabilityModifier;
    }
}