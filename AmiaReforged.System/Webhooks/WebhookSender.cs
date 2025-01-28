using AmiaReforged.Core.Helpers;
using NLog;

namespace AmiaReforged.System.Webhooks;

public class WebhookSender
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly string _webhookUri;

    public WebhookSender(string webhookUri)
    {
        _webhookUri = webhookUri;
    }

    public async Task SendMessage(string username, string message, string avatar = "")
    {
        try
        {
            using HttpClient httpClient = new();
            Dictionary<string, string> postParams = new()
            {
                ["username"] = username,
                ["content"] = message,
                ["avatar_url"] = avatar
            };

            using FormUrlEncodedContent postContent = new(postParams);
            await httpClient.PostAsync(_webhookUri, postContent);
            await new NwTaskHelper().TrySwitchToMainThread();
        }catch(Exception ex)
        {
            Log.Error(ex, "Webhook is likely not correct, or not set");
        }
    }
}