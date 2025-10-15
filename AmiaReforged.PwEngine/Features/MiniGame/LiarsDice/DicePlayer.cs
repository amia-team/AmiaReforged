namespace AmiaReforged.PwEngine.Features.MiniGame.LiarsDice;

public class DicePlayer
{
    public DicePlayer(string name, int diceCount, int buyIn)
    {
        Name = name;
        Dice = DiceFactory.RollDice(diceCount);
        Pot = buyIn;
        HasFolded = false;
    }

    public string Name { get; }
    public List<int> Dice { get; }
    public int Pot { get; }
    public bool HasFolded { get; private set; }
    public bool IsEliminated => Dice.Count == 0 || Pot <= 0;

    public void LoseDie()
    {
        if (Dice.Count > 0) Dice.RemoveAt(0);
    }

    public void Fold()
    {
        HasFolded = true;
        Dice.Clear(); // Dump all dice when folding
    }
}
