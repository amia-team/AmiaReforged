namespace AmiaReforged.PwEngine.Features.Module;

public interface IBlueprintSource
{
    IEnumerable<IBlueprint> GetBlueprints(BlueprintObjectType blueprintType, int start, string search, int count);
}
