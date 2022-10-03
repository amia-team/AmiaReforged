namespace AmiaReforged.System.Webhooks;

public class WebhookSender
{
    private readonly string _webhookUri;

    public WebhookSender(string webhookUri)
    {
        _webhookUri = webhookUri;
    }

    public async Task SendMessage(string username, string message, string avatar = "")
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
    }
}