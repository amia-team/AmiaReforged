namespace AmiaReforged.PwEngine.Systems.MiniGame.LiarsDice;

public interface IGameObserver
{
    void OnGameEvent(GameUpdate message);
}