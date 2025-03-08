namespace AmiaReforged.PwEngine.Systems.Module;

public interface IBlueprintSource
{
    IEnumerable<IBlueprint> GetBlueprints(BlueprintObjectType blueprintType, int start, string search, int count);
}