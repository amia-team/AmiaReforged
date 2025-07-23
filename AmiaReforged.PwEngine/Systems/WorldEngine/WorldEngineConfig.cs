using AmiaReforged.Core;
using AmiaReforged.Core.Models.World;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Database;
using Anvil.Services;
using NLog;
using WorldConfiguration = AmiaReforged.PwEngine.Database.Entities.WorldConfiguration;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

[ServiceBinding(typeof(IWorldConfigProvider))]
public class WorldEngineConfig(PwContextFactory factory) : IWorldConfigProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _ctx = factory.CreateDbContext();

    public bool GetBoolean(string key)
    {
        bool value = false;

        try
        {
            WorldConfiguration? entry = _ctx.WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConfigConstants.ConfigTypeBool);

            if (entry != null) value = bool.Parse(entry.Value);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return value;
    }

    public int? GetInt(string key)
    {
        return null;
    }

    public float? GetFloat(string key) => null;

    public string? GetString(string key) => null;
}
