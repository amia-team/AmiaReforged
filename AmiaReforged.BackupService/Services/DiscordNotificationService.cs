using System.Text;
using System.Text.Json;
using AmiaReforged.BackupService.Configuration;

namespace AmiaReforged.BackupService.Services;

/// <summary>
/// Service for sending Discord webhook notifications.
/// </summary>
public interface IDiscordNotificationService
{
    /// <summary>
    /// Sends an error notification to Discord.
    /// </summary>
    Task SendErrorAsync(string title, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a warning notification to Discord.
    /// </summary>
    Task SendWarningAsync(string title, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a success notification to Discord.
    /// </summary>
    Task SendSuccessAsync(string title, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an info notification to Discord.
    /// </summary>
    Task SendInfoAsync(string title, string message, CancellationToken cancellationToken = default);
}

public class DiscordNotificationService : IDiscordNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DiscordNotificationService> _logger;
    private readonly BackupConfig _config;

    public DiscordNotificationService(
        IHttpClientFactory httpClientFactory,
        ILogger<DiscordNotificationService> logger,
        BackupConfig config)
    {
        _httpClient = httpClientFactory.CreateClient("Discord");
        _logger = logger;
        _config = config;
    }

    public async Task SendErrorAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        await SendWebhookAsync(title, message, 0xFF0000, cancellationToken); // Red
    }

    public async Task SendWarningAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        await SendWebhookAsync(title, message, 0xFFA500, cancellationToken); // Orange
    }

    public async Task SendSuccessAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        await SendWebhookAsync(title, message, 0x00FF00, cancellationToken); // Green
    }

    public async Task SendInfoAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        await SendWebhookAsync(title, message, 0x0099FF, cancellationToken); // Blue
    }

    private async Task SendWebhookAsync(string title, string message, int color, CancellationToken cancellationToken)
    {
        string? webhookUrl = _config.GetDiscordWebhookUrl();

        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogDebug("Discord webhook URL not configured, skipping notification");
            return;
        }

        try
        {
            var payload = new
            {
                embeds = new[]
                {
                    new
                    {
                        title,
                        description = message,
                        color,
                        timestamp = DateTime.UtcNow.ToString("o"),
                        footer = new
                        {
                            text = "Amia Backup Service"
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync(webhookUrl, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send Discord notification: {StatusCode}", response.StatusCode);
            }
            else
            {
                _logger.LogDebug("Discord notification sent successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error sending Discord notification");
            // Don't throw - notifications are best effort
        }
    }
}
