﻿using Anvil.API;

namespace AmiaReforged.Races.Races.Types.HeritageAbilities;

public interface IHeritageAbilities
{
    public void SetupStats(NwPlayer player);
    public void RemoveStats(NwPlayer player);
}
