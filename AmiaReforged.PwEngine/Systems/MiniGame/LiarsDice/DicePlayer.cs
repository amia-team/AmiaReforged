namespace AmiaReforged.PwEngine.Systems.MiniGame.LiarsDice;

    public class DicePlayer
    {
        public string Name { get; }
        public List<int> Dice { get; private set; }
        public int Pot { get; private set; }
        public bool HasFolded { get; private set; }
        public bool IsEliminated => Dice.Count == 0 || Pot <= 0;

        public DicePlayer(string name, int diceCount, int buyIn)
        {
            Name = name;
            Dice = DiceFactory.RollDice(diceCount);
            Pot = buyIn;
            HasFolded = false;
        }

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