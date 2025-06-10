namespace AmiaReforged.PwEngine.Systems.MiniGame.LiarsDice;

public class LiarsDiceGame
{
    private readonly Dictionary<DicePlayer, (int quantity, int faceValue)?> _bids;
    private readonly List<IGameObserver> _observers;
    private readonly List<DicePlayer> _players;

    public LiarsDiceGame(List<(string name, bool isAI, int buyIn)> playerData)
    {
        _players = playerData.Select(pd => new DicePlayer(pd.name, 5, pd.buyIn)).ToList();
        _observers = new();
        _bids = new Dictionary<DicePlayer, (int, int)?>();
    }

    public void AddObserver(IGameObserver observer)
    {
        _observers.Add(observer);
    }

    private void NotifyObservers(GameUpdate update, DicePlayer? recipient = null)
    {
        foreach (var observer in _observers)
        {
            if (recipient == null || recipient == update.Recipient) observer.OnGameEvent(update);
        }
    }

    public void StartGame()
    {
        NotifyObservers(new(message: "Game has started!"));
        BeginBiddingPhase();
    }

    private async void BeginBiddingPhase()
    {
        NotifyObservers(new(message: "Players are placing bids..."));
        await Task.Delay(5000); // Simulated async bidding phase
        NotifyObservers(new(message: "All bids are in!"));
        ResolveBidding();
    }

    public void PlaceBid(DicePlayer dicePlayer, int quantity, int faceValue)
    {
        if (!_players.Contains(dicePlayer) || dicePlayer.HasFolded || dicePlayer.IsEliminated)
            return;

        _bids[dicePlayer] = (quantity, faceValue);
        NotifyObservers(new($"{dicePlayer.Name} has placed a bid."));
    }

    public void Fold(DicePlayer dicePlayer)
    {
        if (!_players.Contains(dicePlayer) || dicePlayer.HasFolded || dicePlayer.IsEliminated)
            return;

        dicePlayer.Fold();
        NotifyObservers(new($"{dicePlayer.Name} has folded."));
    }

    private void ResolveBidding()
    {
        if (_bids.Count == 0)
        {
            NotifyObservers(new(message: "No valid bids. Game over."));
            return;
        }

        var lastBid = _bids.Values.LastOrDefault();
        if (!lastBid.HasValue) return;

        int totalCount = _players.Sum(p => p.Dice.Count(d => d == lastBid.Value.faceValue));
        DicePlayer lastBidder = _bids.Last().Key;

        if (totalCount >= lastBid.Value.quantity)
        {
            NotifyObservers(new($"Challenge failed! {lastBidder.Name} wins this round."));
        }
        else
        {
            NotifyObservers(new($"Challenge successful! {lastBidder.Name} loses a die."));
            lastBidder.LoseDie();
        }

        if (_players.Count(p => !p.IsEliminated) == 1)
        {
            NotifyObservers(new($"{_players.First(p => !p.IsEliminated).Name} wins!"));
        }
        else
        {
            _bids.Clear();
            BeginBiddingPhase();
        }
    }
}