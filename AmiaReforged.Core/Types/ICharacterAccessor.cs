﻿using AmiaReforged.Core.Models;

namespace AmiaReforged.Core.Types;

public interface ICharacterAccessor
{
    IReadOnlyList<AmiaCharacter> GetCharacters(string publicCdKey);
    void AddCharacter(string publicCdKey, AmiaCharacter character);
}