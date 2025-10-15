namespace AmiaReforged.PwEngine.Features.WorldEngine;

public interface IWorldConfigProvider
{
    public bool GetBoolean(string key);
    public int? GetInt(string key);
    public float? GetFloat(string key);
    public string? GetString(string key);
    void SetBoolean(string economyInitialized, bool b);
}
