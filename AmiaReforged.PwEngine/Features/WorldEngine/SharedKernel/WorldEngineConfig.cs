using AmiaReforged.PwEngine.Database;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;
using WorldConfiguration = AmiaReforged.PwEngine.Database.Entities.WorldConfiguration;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

[ServiceBinding(typeof(IWorldConfigProvider))]
public class WorldEngineConfig(PwContextFactory factory) : IWorldConfigProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public bool GetBoolean(string key)
    {
        bool value = false;

        try
        {
            WorldConfiguration? entry = factory.CreateDbContext().WorldConfiguration.FirstOrDefault(b =>
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
            PwEngineContext ctx = factory.CreateDbContext();
            WorldConfiguration? entry = ctx.WorldConfiguration.FirstOrDefault(b =>
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
            ctx.Update(entry);
            ctx.SaveChanges();
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
            PwEngineContext ctx = factory.CreateDbContext();
            ctx.Add(entry);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public int? GetInt(string key)
    {
        try
        {
            WorldConfiguration? entry = factory.CreateDbContext().WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeInt);

            if (entry != null && int.TryParse(entry.Value, out int value)) return value;
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return null;
    }

    public void SetInt(string key, int value)
    {
        try
        {
            PwEngineContext ctx = factory.CreateDbContext();
            WorldConfiguration? entry = ctx.WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeInt);

            if (entry == null)
            {
                ctx.Add(new WorldConfiguration
                {
                    Key = key,
                    Value = value.ToString(),
                    ValueType = WorldConstants.ConfigTypeInt
                });
            }
            else
            {
                entry.Value = value.ToString();
                ctx.Update(entry);
            }

            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public float? GetFloat(string key)
    {
        try
        {
            WorldConfiguration? entry = factory.CreateDbContext().WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeFloat);

            if (entry != null && float.TryParse(entry.Value, System.Globalization.CultureInfo.InvariantCulture,
                    out float value))
                return value;
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return null;
    }

    public void SetFloat(string key, float value)
    {
        try
        {
            PwEngineContext ctx = factory.CreateDbContext();
            WorldConfiguration? entry = ctx.WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeFloat);

            if (entry == null)
            {
                ctx.Add(new WorldConfiguration
                {
                    Key = key,
                    Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ValueType = WorldConstants.ConfigTypeFloat
                });
            }
            else
            {
                entry.Value = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
                ctx.Update(entry);
            }

            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public string? GetString(string key)
    {
        try
        {
            WorldConfiguration? entry = factory.CreateDbContext().WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeString);

            return entry?.Value;
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return null;
    }

    public void SetString(string key, string value)
    {
        try
        {
            PwEngineContext ctx = factory.CreateDbContext();
            WorldConfiguration? entry = ctx.WorldConfiguration.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConstants.ConfigTypeString);

            if (entry == null)
            {
                ctx.Add(new WorldConfiguration
                {
                    Key = key,
                    Value = value,
                    ValueType = WorldConstants.ConfigTypeString
                });
            }
            else
            {
                entry.Value = value;
                ctx.Update(entry);
            }

            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }
}
