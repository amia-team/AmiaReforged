namespace AmiaReforged.PwEngine.Features.MiniGame.LiarsDice;

public interface IGameObserver
{
    void OnGameEvent(GameUpdate message);
}
