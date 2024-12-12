using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.System.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]

public class SetSoundset : IChatCommand
{
    public string Command => $"./setsoundset";

    public Task ExecuteCommand(NwPlayer caller, string message)
    {
        if (!caller.IsDM) return Task.CompletedTask;
        try
        {
            int soundsetId = int.Parse(message.Split(' ')[1]);
            if (soundsetId > 467)
            {
                caller.SendServerMessage("Invalid input. Soundset id can't go beyond 467.");
                return Task.CompletedTask;
            }
            caller.ControlledCreature.GetObjectVariable<LocalVariableInt>("soundsetid").Value = soundsetId;
            caller.EnterTargetMode(SetCreatureSoundset, new TargetModeSettings { ValidTargets = ObjectTypes.Creature });
            caller.FloatingTextString($"Setting soundset id {soundsetId}!", false);
            return Task.CompletedTask;
        }
        catch 
        {
            caller.SendServerMessage(
                "Usage: \"./setsoundset <soundset id>\". Check here for soundset id: https://www.amiaworld.com/phpbb/viewtopic.php?p=33052");
        }
        return Task.CompletedTask;
    }

    private void SetCreatureSoundset(ModuleEvents.OnPlayerTarget obj)
    {
        NwCreature targetCreature = (NwCreature)obj.TargetObject;
        int soundsetId = targetCreature.GetObjectVariable<LocalVariableInt>("soundsetid").Value;

        targetCreature.SoundSet = (ushort)soundsetId;
        targetCreature.PlayVoiceChat(VoiceChatType.BattleCry1);
    }
}