namespace AmiaReforged.PwEngine.Systems.MiniGame.LiarsDice;

public class GameUpdate
{
    public GameUpdateEnum Notification { get; }
    public string Message { get; }
    public DicePlayer? Recipient { get; }

    public GameUpdate(GameUpdateEnum notification, string message, DicePlayer? recipient = null)
    {
        Notification = notification;
        Message = message;
        Recipient = recipient;
    }
}