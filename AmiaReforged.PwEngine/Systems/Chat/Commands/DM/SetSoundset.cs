using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.Chat.Commands.DM;

[ServiceBinding(typeof(IChatCommand))]
public class SetSoundset : IChatCommand
{
    public string Command => "./setsoundset";

    public Task ExecuteCommand(NwPlayer caller, string[] args)
    {
        if (!caller.IsDM) return Task.CompletedTask;
        try
        {
            int soundsetId = int.Parse(args[0]);
            if (soundsetId > 467)
            {
                caller.SendServerMessage(message: "Invalid input. Soundset id can't go beyond 467.");
                return Task.CompletedTask;
            }

            if(caller.ControlledCreature == null) return Task.CompletedTask;
            
            caller.ControlledCreature.GetObjectVariable<LocalVariableInt>(name: "soundsetid").Value = soundsetId;
            caller.EnterTargetMode(SetCreatureSoundset, new TargetModeSettings { ValidTargets = ObjectTypes.Creature });
            caller.FloatingTextString($"Setting soundset id {soundsetId}!", false);
            return Task.CompletedTask;
        }
        catch
        {
            caller.SendServerMessage(
                message:
                "Usage: \"./setsoundset <soundset id>\". Check here for soundset id: https://www.amiaworld.com/phpbb/viewtopic.php?p=33052");
        }

        return Task.CompletedTask;
    }

    private void SetCreatureSoundset(ModuleEvents.OnPlayerTarget obj)
    {
        if(obj.TargetObject is not NwCreature targetCreature) return;
        
        int soundsetId = targetCreature.GetObjectVariable<LocalVariableInt>(name: "soundsetid").Value;

        targetCreature.SoundSet = (ushort)soundsetId;
        targetCreature.PlayVoiceChat(VoiceChatType.BattleCry1);
    }
}