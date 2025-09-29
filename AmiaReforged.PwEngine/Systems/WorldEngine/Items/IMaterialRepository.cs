namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

public interface IMaterialRepository
{
    void Upsert(Material material);
    void Remove(Material material);
    IList<Material> All();
    Material? Get(MaterialEnum e);
}
