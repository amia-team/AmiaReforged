using AmiaReforged.Core;
using AmiaReforged.Core.Models.World;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Database;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;
using WorldConfiguration = AmiaReforged.PwEngine.Database.Entities.WorldConfiguration;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

// [ServiceBinding(typeof(IWorldConfigProvider))]
public class WorldEngineConfig : IWorldConfigProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _ctx;

    public WorldEngineConfig(PwContextFactory factory)
    {
        string environment = UtilPlugin.GetEnvironmentVariable(sVarname: "SERVER_MODE");

        if (environment == "live") return;

        _ctx = factory.CreateDbContext();
    }

    public bool GetBoolean(string key)
    {
        bool value = false;

        try
        {
            WorldConfiguration? entry = _ctx.WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeBool);

            if (entry != null) value = bool.Parse(entry.Value);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return value;
    }

    public void SetBoolean(string key, bool value)
    {
        try
        {
            WorldConfiguration? entry = _ctx.WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeBool);

            bool exists = entry != null;
            if (!exists)
            {
                AddBoolean(new WorldConfiguration
                {
                    Key = key,
                    Value = value.ToString(),
                    ValueType = WorldConstants.ConfigTypeBool
                });

                return;
            }

            entry!.Value = value.ToString();
            _ctx.Update(entry);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    private void AddBoolean(WorldConfiguration entry)
    {
        try
        {
            _ctx.Add(entry);
            _ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public int? GetInt(string key)
    {
        return null;
    }

    public float? GetFloat(string key) => null;

    public string? GetString(string key) => null;
}
