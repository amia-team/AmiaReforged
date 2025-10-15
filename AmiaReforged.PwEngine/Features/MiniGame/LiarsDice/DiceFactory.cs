namespace AmiaReforged.PwEngine.Features.MiniGame.LiarsDice;
// Observer Interface for game events

// Dice Factory for flexibility (e.g., different dice types)
public static class DiceFactory
{
    public static List<int> RollDice(int count, int sides = 6)
    {
        Random rand = new();
        return Enumerable.Range(0, count).Select(_ => rand.Next(1, sides + 1)).ToList();
    }
}

// Player Class

// AI Player Class

// Game State and Logic

// Example Observer (e.g., Console Logger, GUI Hook, etc.)
