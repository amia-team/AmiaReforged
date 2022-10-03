using AmiaReforged.System.Webhooks;
using NUnit.Framework;

namespace AmiaReforged.System.Tests;

[TestFixture]
public class WebhookTest
{
    private WebhookSender _webhookSender;

    [SetUp]
    public void SetUp()
    {
        _webhookSender =
            new WebhookSender(
                "https://discord.com/api/webhooks/967871266356854864/hAV6hfaX2TY4OjMZK63ffC47-e_j8ijLxey_3_Ku8mGRarcsjiaeatQAbrFTaJq37ZOs");
    }

    [Test]
    public async Task TestWebhook()
    {
        await _webhookSender.SendMessage("TestBoi", "Test message\n\t\tFart", "https://i.imgur.com/UmHQ3fG.png");
    }
}