using Anvil.API;
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
                [key: "username"] = username,
                [key: "content"] = message,
                [key: "avatar_url"] = avatar
            };

            using FormUrlEncodedContent postContent = new(postParams);
            await httpClient.PostAsync(_webhookUri, postContent);
            await NwTask.SwitchToMainThread();
        }
        catch (Exception ex)
        {
            Log.Error(ex, message: "Webhook is likely not correct, or not set");
        }
    }
}
