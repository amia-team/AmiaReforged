using Discord;
using Discord.Webhook;
using WorldSimulator.Domain.Events;

namespace WorldSimulator.Infrastructure.Services;

/// <summary>
/// Internal structure for queuing event log entries
/// </summary>
internal record EventLogEntry
{
    public required SimulationEvent Event { get; init; }
    public required EventSeverity Severity { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Service that publishes simulation events to Discord webhooks.
/// Can be toggled on/off at runtime via configuration.
/// </summary>
public class DiscordEventLogService : IEventLogPublisher, IHostedService, IDisposable
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<DiscordEventLogService> _logger;
    private readonly Channel<EventLogEntry> _eventChannel;
    private DiscordWebhookClient? _webhookClient;
    private bool _isEnabled;
    private CancellationTokenSource? _cts;
    private Task? _processingTask;

    public DiscordEventLogService(
        IConfiguration configuration,
        ILogger<DiscordEventLogService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _eventChannel = Channel.CreateUnbounded<EventLogEntry>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Discord Event Log Service");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        RefreshConfiguration();
        _processingTask = Task.Run(() => ProcessEventQueue(_cts.Token), _cts.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Discord Event Log Service");
        _eventChannel.Writer.Complete();

        if (_processingTask != null)
        {
            await _processingTask;
        }

        _webhookClient?.Dispose();
    }

    public void RefreshConfiguration()
    {
        _isEnabled = _configuration.GetValue<bool>("Discord:Enabled");
        string? webhookUrl = _configuration["Discord:WebhookUrl"];

        if (_isEnabled && !string.IsNullOrEmpty(webhookUrl))
        {
            _webhookClient?.Dispose();
            _webhookClient = new DiscordWebhookClient(webhookUrl);
            _logger.LogInformation("Discord webhook configured and enabled");
        }
        else
        {
            _logger.LogInformation("Discord webhook disabled or not configured");
        }
    }

    public async Task PublishAsync(SimulationEvent eventData, EventSeverity severity = EventSeverity.Information, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            _logger.LogTrace("Discord publishing disabled, skipping event {EventType}", eventData.GetType().Name);
            return;
        }

        EventLogEntry entry = new EventLogEntry
        {
            Event = eventData,
            Severity = severity,
            Timestamp = DateTimeOffset.UtcNow
        };

        await _eventChannel.Writer.WriteAsync(entry);
    }

    private async Task ProcessEventQueue(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord event queue processor started");

        await foreach (EventLogEntry entry in _eventChannel.Reader.ReadAllAsync(cancellationToken))
        {
            if (!_isEnabled || _webhookClient == null)
            {
                continue;
            }

            try
            {
                Embed embed = CreateEmbed(entry);
                await _webhookClient.SendMessageAsync(embeds: new[] { embed });
                _logger.LogTrace("Published {EventType} to Discord", entry.Event.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Discord webhook for event {EventType}",
                    entry.Event.GetType().Name);
            }
        }

        _logger.LogInformation("Discord event queue processor stopped");
    }

    private Embed CreateEmbed(EventLogEntry entry)
    {
        EmbedBuilder? embedBuilder = new EmbedBuilder()
            .WithTitle($"🎲 {entry.Event.GetType().Name}")
            .WithDescription(entry.Event.Message)
            .WithColor(GetColorForSeverity(entry.Severity))
            .WithTimestamp(entry.Timestamp)
            .WithFooter($"Occurred at {entry.Event.OccurredAt:yyyy-MM-dd HH:mm:ss} UTC");

        // Add environment field
        string environment = _configuration["ENVIRONMENT_NAME"] ?? "Unknown";
        embedBuilder.AddField("Environment", environment, inline: true);

        // Add event-specific fields based on type
        AddEventSpecificFields(embedBuilder, entry.Event);

        return embedBuilder.Build();
    }

    private void AddEventSpecificFields(EmbedBuilder builder, SimulationEvent evt)
    {
        switch (evt)
        {
            case CircuitBreakerStateChanged cb:
                builder.AddField("State", cb.State, inline: true);
                builder.AddField("Host", cb.Host, inline: true);
                if (!string.IsNullOrEmpty(cb.Error))
                    builder.AddField("Error", TruncateField(cb.Error), inline: false);
                break;

            case WorkItemCompleted wc:
                builder.AddField("Work Type", wc.Type, inline: true);
                builder.AddField("Duration", $"{wc.Duration.TotalSeconds:F2}s", inline: true);
                break;

            case WorkItemFailed wf:
                builder.AddField("Work Type", wf.Type, inline: true);
                builder.AddField("Retry Count", wf.RetryCount.ToString(), inline: true);
                builder.AddField("Error", TruncateField(wf.Error), inline: false);
                break;

            case WorkItemQueued wq:
                builder.AddField("Work Type", wq.Type, inline: true);
                break;

            case SimulationServiceStarted ss:
                builder.AddField("Environment", ss.Environment, inline: true);
                break;

            case SimulationServiceStopping sss:
                builder.AddField("Reason", sss.Reason, inline: true);
                break;

            default:
                // For unknown events, serialize to JSON
                builder.AddField("Details", TruncateField(JsonSerializer.Serialize(evt)), inline: false);
                break;
        }
    }

    private static string TruncateField(string value, int maxLength = 1024)
    {
        if (value.Length <= maxLength)
            return value;

        return value[..(maxLength - 3)] + "...";
    }

    private static Color GetColorForSeverity(EventSeverity severity) => severity switch
    {
        EventSeverity.Critical => Color.Red,
        EventSeverity.Warning => Color.Gold,
        EventSeverity.Information => Color.Blue,
        _ => Color.Default
    };

    public void Dispose()
    {
        _cts?.Dispose();
        _webhookClient?.Dispose();
    }
}


