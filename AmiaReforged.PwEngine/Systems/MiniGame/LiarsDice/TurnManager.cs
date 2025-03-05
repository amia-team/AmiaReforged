namespace AmiaReforged.PwEngine.Systems.MiniGame.LiarsDice;

public class TurnManager
{
    private readonly List<DicePlayer> _players;
    private int _currentPlayerIndex;

    public TurnManager(List<DicePlayer> players)
    {
        _players = players;
        _currentPlayerIndex = 0;
    }

    public DicePlayer GetCurrentPlayer() => _players[_currentPlayerIndex];

    public void NextTurn()
    {
        do
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _players.Count;
        } while (_players[_currentPlayerIndex].IsEliminated);
    }
}