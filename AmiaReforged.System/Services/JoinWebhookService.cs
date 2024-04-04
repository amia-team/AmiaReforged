using AmiaReforged.System.Webhooks;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.System.Services;

[ServiceBinding(typeof(JoinWebhookService))]
public class JoinWebhookService
{
    private const string Avatar = "https://i.imgur.com/UmHQ3fG.png";
    private const string Username = "Savras";
    private const string GenericAvatar = "https://imgur.com/ScFFxch.png";
    private const string GenericUsername = "Timat";
    private readonly SchedulerService _schedulerService;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly WebhookSender _webhookSender;
    private readonly WebhookSender _webhookSenderGeneric;

    public JoinWebhookService(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService;
        _schedulerService.ScheduleRepeating(ListPlayers, TimeSpan.FromMinutes(30));
        _webhookSender = new WebhookSender(UtilPlugin.GetEnvironmentVariable("SERVER_NOTIFICATION_WEBHOOK"));
        Log.Info("JoinWebhook Service Initialized.");
    }

    public JoinWebhookService()
    {
        _webhookSenderGeneric = new WebhookSender(UtilPlugin.GetEnvironmentVariable("SERVER_NOTIFICATION_GENERIC"));
        Log.Info("JoinWebhook Service Initialized.");
    }

    private async void ListPlayers()
    {
        bool noPlayers = !NwModule.Instance.Players.Any();

        if (noPlayers)
        {
            await _webhookSender.SendMessage(Username, "There are no players online.", Avatar);
            return;
        }

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        string players = NwModule.Instance.Players.Where(player => player.LoginCreature != null).Aggregate("Players online: \n", (current, player) => current + $"\t\t{player.PlayerName} - {player.LoginCreature.Name}\n");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        await _webhookSender.SendMessage(Username, players, Avatar);
    }
    
    public async Task LaunchDiscordMessage(string messageSent)
    {
       await _webhookSenderGeneric.SendMessage(GenericUsername,messageSent,GenericAvatar); 
    }
}


