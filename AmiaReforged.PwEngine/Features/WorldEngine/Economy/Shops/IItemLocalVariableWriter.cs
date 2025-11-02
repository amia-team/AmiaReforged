namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface IItemLocalVariableWriter
{
    void SetInt(string name, int value);
    void SetString(string name, string value);
    void SetJson(string name, string json);
}
