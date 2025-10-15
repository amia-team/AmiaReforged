using Anvil.API.Events;

namespace AmiaReforged.PwEngine.Features.AI.Behaviors;

public interface IOnUserDefined
{
    string ScriptName { get; }

    void UserDefined(CreatureEvents.OnUserDefined eventData);
}
