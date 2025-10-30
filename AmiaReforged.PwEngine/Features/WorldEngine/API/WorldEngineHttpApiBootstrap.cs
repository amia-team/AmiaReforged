using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API;

/// <summary>
/// Bootstraps the WorldEngine HTTP API on service initialization.
/// Anvil services are initialized when the module loads.
/// </summary>
[ServiceBinding(typeof(WorldEngineHttpApiBootstrap))]
public class WorldEngineHttpApiBootstrap : IDisposable
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly WorldEngineHttpServer _httpServer;

    public WorldEngineHttpApiBootstrap()
    {
        _logger.Info("Bootstrapping WorldEngine HTTP API...");

        try
        {
            // Get configuration from environment or config
            var port = GetConfigInt("WORLDENGINE_API_PORT", 8080);
            var apiKey = GetConfigString("WORLDENGINE_API_KEY", "dev-api-key-change-in-production");

            // Create router
            var router = new WorldEngineApiRouter(_logger);

            // Create and start server
            _httpServer = new WorldEngineHttpServer(router, _logger, apiKey, port);
            _httpServer.Start();

            _logger.Info("WorldEngine HTTP API ready ✓");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start WorldEngine HTTP API");
            throw;
        }
    }

    private int GetConfigInt(string key, int defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    private string GetConfigString(string key, string defaultValue)
    {
        return Environment.GetEnvironmentVariable(key) ?? defaultValue;
    }

    public void Dispose()
    {
        _logger.Info("Shutting down WorldEngine HTTP API...");

        try
        {
            _httpServer.Dispose();
            _logger.Info("WorldEngine HTTP API shut down successfully");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during HTTP API shutdown");
        }
    }
}

