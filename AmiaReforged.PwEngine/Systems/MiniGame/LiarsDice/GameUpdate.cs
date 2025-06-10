namespace AmiaReforged.PwEngine.Systems.MiniGame.LiarsDice;

public class GameUpdate
{
    public GameUpdate(string message, DicePlayer? recipient = null)
    {
        Recipient = recipient;
    }

    public DicePlayer? Recipient { get; }
}