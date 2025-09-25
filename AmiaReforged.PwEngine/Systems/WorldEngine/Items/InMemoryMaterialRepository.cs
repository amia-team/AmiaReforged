using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

/// <summary>
/// An in-memory implementation of the <see cref="IMaterialRepository"/> interface.
/// This repository manages materials keyed by their <see cref="MaterialEnum"/> values.
/// </summary>
[ServiceBinding(typeof(IMaterialRepository))]
public class InMemoryMaterialRepository : IMaterialRepository
{
    private readonly Dictionary<MaterialEnum, Material> _materials = new();

    /// <summary>
    /// Inserts a new material or updates an existing material in the repository.
    /// </summary>
    /// <param name="material">The material to add or update in the repository.</param>
    public void Upsert(Material material)
    {
        if (!_materials.TryAdd(material.Enum, material))
        {
            _materials[material.Enum] = material;
        }
    }

    /// <summary>
    /// Removes the specified <paramref name="material"/> from the repository.
    /// </summary>
    /// <param name="material">The material to remove from the repository.</param>
    public void Remove(Material material) => _materials.Remove(material.Enum);

    /// <summary>
    /// Retrieves a list of all materials stored in the repository.
    /// </summary>
    /// <returns>A list of all <see cref="Material"/> instances in the repository.</returns>
    public IList<Material> All() => _materials.Values.ToList();

    /// Retrieves a material based on the given material enumeration value.
    /// If the material is not found, it returns null.
    /// <param name="e">The enumeration value of the material to retrieve.</param>
    /// <returns>The material associated with the specified enumeration value, or null if not found.</returns>
    public Material? Get(MaterialEnum e) => _materials.GetValueOrDefault(e);
}
