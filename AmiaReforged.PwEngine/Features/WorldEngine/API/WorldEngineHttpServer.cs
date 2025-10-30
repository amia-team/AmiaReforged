using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.API;

/// <summary>
/// Lightweight HTTP server for WorldEngine REST API.
/// Uses HttpListener since ASP.NET Core doesn't work with Anvil.
/// </summary>
public class WorldEngineHttpServer : IDisposable
{
    private readonly HttpListener _listener;
    private readonly IApiRouter _router;
    private readonly Logger _logger;
    private readonly CancellationTokenSource _cts;
    private readonly string _apiKey;
    private bool _isRunning;

    public WorldEngineHttpServer(
        IApiRouter router,
        Logger logger,
        string apiKey,
        int port = 8080)
    {
        _router = router ?? throw new ArgumentNullException(nameof(router));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _cts = new CancellationTokenSource();

        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/api/worldengine/");

        _logger.Info("WorldEngineHttpServer initialized on port {Port}", port);
    }

    public void Start()
    {
        if (_isRunning)
        {
            _logger.Warn("HTTP server already running");
            return;
        }

        try
        {
            _listener.Start();
            _isRunning = true;

            _logger.Info("WorldEngine HTTP API started on {Prefixes}",
                string.Join(", ", _listener.Prefixes));

            // Start listening loop in background
            Task.Run(ListenAsync, _cts.Token);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start HTTP server");
            throw;
        }
    }

    private async Task ListenAsync()
    {
        _logger.Info("HTTP listener loop started");

        while (!_cts.Token.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();

                // Handle each request in a separate task (non-blocking)
                _ = Task.Run(() => HandleRequestAsync(context), _cts.Token);
            }
            catch (HttpListenerException) when (_cts.Token.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error accepting HTTP request");
            }
        }

        _logger.Info("HTTP listener loop stopped");
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var sw = Stopwatch.StartNew();
        var request = context.Request;
        var response = context.Response;

        var method = request.HttpMethod;
        var path = request.Url?.AbsolutePath ?? "/";
        var correlationId = Guid.NewGuid().ToString("N")[..8];

        try
        {
            _logger.Debug("[{CorrelationId}] HTTP {Method} {Path} started",
                correlationId, method, path);

            // Validate API key
            if (!ValidateApiKey(request))
            {
                await WriteResponseAsync(response, 401,
                    new ErrorResponse("Unauthorized", "Invalid or missing API key"));
                return;
            }

            // Route to handler
            var result = await _router.RouteAsync(method, path, request, _cts.Token);

            // Write response
            await WriteResponseAsync(response, result.StatusCode, result.Data);

            _logger.Info("[{CorrelationId}] HTTP {Method} {Path} completed in {Duration}ms with {StatusCode}",
                correlationId, method, path, sw.ElapsedMilliseconds, result.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "[{CorrelationId}] Unhandled exception in HTTP request handler",
                correlationId);

            await WriteResponseAsync(response, 500,
                new ErrorResponse("Internal server error", ex.Message));
        }
        finally
        {
            sw.Stop();
            response.Close();
        }
    }

    private bool ValidateApiKey(HttpListenerRequest request)
    {
        // Check X-API-Key header
        var providedKey = request.Headers["X-API-Key"];

        if (string.IsNullOrEmpty(providedKey))
        {
            _logger.Warn("Request received without API key from {RemoteEndPoint}",
                request.RemoteEndPoint);
            return false;
        }

        if (providedKey != _apiKey)
        {
            _logger.Warn("Request received with invalid API key from {RemoteEndPoint}",
                request.RemoteEndPoint);
            return false;
        }

        return true;
    }

    private async Task WriteResponseAsync(HttpListenerResponse response, int statusCode, object data)
    {
        response.StatusCode = statusCode;
        response.ContentType = "application/json";
        response.Headers.Add("X-Powered-By", "WorldEngine/1.0");

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        var buffer = Encoding.UTF8.GetBytes(json);
        response.ContentLength64 = buffer.Length;

        await response.OutputStream.WriteAsync(buffer, _cts.Token);
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _logger.Info("Stopping WorldEngine HTTP API...");

        _cts.Cancel();
        _listener.Stop();
        _isRunning = false;

        _logger.Info("WorldEngine HTTP API stopped");
    }

    public void Dispose()
    {
        Stop();
        _listener.Close();
        _cts.Dispose();
    }
}

/// <summary>
/// Standard API result wrapper
/// </summary>
public record ApiResult(int StatusCode, object Data);

/// <summary>
/// Standard error response
/// </summary>
public record ErrorResponse(string Error, string? Details = null, int? Code = null);

