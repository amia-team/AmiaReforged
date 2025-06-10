using AmiaReforged.System.Webhooks;
using NUnit.Framework;

namespace AmiaReforged.System.Tests;

[TestFixture]
public class WebhookTest
{
    [SetUp]
    public void SetUp()
    {
        _webhookSender =
            new(
                webhookUri:
                "https://discord.com/api/webhooks/967871266356854864/hAV6hfaX2TY4OjMZK63ffC47-e_j8ijLxey_3_Ku8mGRarcsjiaeatQAbrFTaJq37ZOs");
    }

    private WebhookSender _webhookSender = null!;

    [Test]
    public async Task TestWebhook()
    {
        await _webhookSender.SendMessage(username: "TestBoi", message: "Test message\n\t\tFart",
            avatar: "https://i.imgur.com/UmHQ3fG.png");
    }
}