using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Systems.AI.PackageDefinitions;

public interface IOnConversationBehavior
{
    string ScriptName { get; }
    void OnConversation(CreatureEvents.OnConversation eventData);
}