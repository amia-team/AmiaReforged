﻿using Anvil.API;

namespace AmiaReforged.PwEngine.Features.Chat.Commands;

public interface IChatCommand
{
    string Command { get; }
    string Description { get; }
    string AllowedRoles { get; } // "All", "DM", "Player"
    Task ExecuteCommand(NwPlayer caller, string[] args);
}
